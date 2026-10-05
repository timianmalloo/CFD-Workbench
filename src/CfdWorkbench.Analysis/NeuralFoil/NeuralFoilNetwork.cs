using System.Security.Cryptography;
using System.Text;
using CfdWorkbench.Core;

namespace CfdWorkbench.Analysis.NeuralFoil;

public sealed record NeuralFoilPrediction(double AnalysisConfidence, double Cl, double Cd, double Cm,
    double XtrUpper, double XtrLower);

/// <summary>NeuralFoil 0.3.2 xxxlarge, in-process inference. The only runtime dependency is the verified embedded resource.</summary>
public sealed class NeuralFoilNetwork
{
    public const string ResourceName = "CfdWorkbench.Analysis.NeuralFoil.weights.bin";
    public const string SourceSha256 = "94638c04bca3c303515cf0c2944e2b09f587818cbd2860183bd85f2fbeeef428";
    public const string DistributionSha256 = "63a33149c902ad01ecf537dd2d127d9e7ffbf86527893f4dc76f25f7087a3573";
    public const string WeightsSha256 = "e037bd2b92d5a964ffcda33ce0ea39af9d8807bcbb07fe182fb5ec1f1815c689";
    public const int WeightsBytes = 5717832;
    public const string MethodId = "NeuralFoil";
    public const string MethodVersion = "NeuralFoil-0.3.2/xxxlarge/94638c04/weights=5717832B";

    private sealed record Layer(int Rows, int Columns, float[] Matrix, float[] Bias);
    private static readonly Lazy<NeuralFoilNetwork> Embedded = new(LoadEmbedded);
    private readonly Layer[] layers;
    private readonly float[] mean;
    private readonly double[] inverseCovariance;

    private NeuralFoilNetwork(Layer[] layers, float[] mean, double[] inverseCovariance)
    {
        this.layers = layers;
        this.mean = mean;
        this.inverseCovariance = inverseCovariance;
    }

    public static NeuralFoilNetwork FromEmbedded() => Embedded.Value;

    private static NeuralFoilNetwork LoadEmbedded()
    {
        using Stream stream = typeof(NeuralFoilNetwork).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new ContractError("ANA-POLAR-WEIGHTS-MISSING", "The NeuralFoil weights resource is not in the Analysis assembly.");
        using var copy = new MemoryStream(WeightsBytes);
        stream.CopyTo(copy);
        return FromBytes(copy.ToArray());
    }

    public static NeuralFoilNetwork FromBytes(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        string actualHash = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (bytes.Length != WeightsBytes || actualHash != WeightsSha256)
            throw new ContractError("ANA-POLAR-WEIGHTS-HASH", "NeuralFoil weights failed their SHA-256 check.");
        using var reader = new BinaryReader(new MemoryStream(bytes, writable: false), Encoding.ASCII);
        if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "NFPRB1\0\0" || reader.ReadInt32() != 7)
            throw new ContractError("ANA-POLAR-WEIGHTS-FORMAT", "NeuralFoil weight header or layer count is invalid.");
        int[] shape = [25, 512, 512, 512, 512, 512, 512, 198];
        var layers = new Layer[7];
        for (int i = 0; i < layers.Length; i++)
        {
            int rows = reader.ReadInt32(), columns = reader.ReadInt32();
            if (rows != shape[i + 1] || columns != shape[i])
                throw new ContractError("ANA-POLAR-WEIGHTS-FORMAT", "NeuralFoil layer dimensions are invalid.");
            var matrix = new float[rows * columns];
            var bias = new float[rows];
            for (int j = 0; j < matrix.Length; j++) matrix[j] = reader.ReadSingle();
            for (int j = 0; j < bias.Length; j++) bias[j] = reader.ReadSingle();
            layers[i] = new Layer(rows, columns, matrix, bias);
        }
        var mean = new float[25];
        var inverse = new double[25 * 25];
        for (int i = 0; i < mean.Length; i++) mean[i] = reader.ReadSingle();
        for (int i = 0; i < inverse.Length; i++) inverse[i] = reader.ReadDouble();
        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new ContractError("ANA-POLAR-WEIGHTS-FORMAT", "NeuralFoil weights have trailing bytes.");
        return new(layers, mean, inverse);
    }

    public NeuralFoilPrediction Predict(CstParameters cst, double alphaDeg, double reynolds, double ncrit)
    {
        ArgumentNullException.ThrowIfNull(cst);
        if (cst.Upper.Length != 8 || cst.Lower.Length != 8 || !double.IsFinite(alphaDeg) ||
            !double.IsFinite(reynolds) || reynolds <= 0 || !double.IsFinite(ncrit) || ncrit <= 0)
            throw new ContractError("ANA-POLAR-INPUT", "NeuralFoil needs eight CST weights per side and finite flow inputs.");
        Span<double> input = stackalloc double[25];
        cst.Upper.CopyTo(input);
        cst.Lower.CopyTo(input[8..]);
        double angle = VortexLattice.ToRadians(alphaDeg);
        input[16] = cst.LeadingEdge;
        input[17] = cst.TrailingEdge * 50;
        input[18] = Math.Sin(2 * angle);
        input[19] = Math.Cos(angle);
        input[20] = 1 - input[19] * input[19];
        input[21] = (Math.Log(reynolds) - 12.5) / 3.5;
        input[22] = (ncrit - 9) / 4.5;
        input[23] = input[24] = 1;
        Span<double> first = stackalloc double[198];
        Forward(input, first);
        first[0] -= Mahalanobis(input) / 50;
        Span<double> flipped = stackalloc double[25];
        input.CopyTo(flipped);
        for (int i = 0; i < 8; i++)
        {
            flipped[i] = -input[8 + i];
            flipped[8 + i] = -input[i];
        }
        flipped[16] = -input[16];
        flipped[18] = -input[18];
        Span<double> second = stackalloc double[198];
        Forward(flipped, second);
        second[0] -= Mahalanobis(flipped) / 50;
        double rawConfidence = (first[0] + second[0]) / 2;
        double limit = -Math.Log(10 / double.MaxValue);
        double confidence = 1 / (1 + Math.Exp(-Math.Clamp(rawConfidence, -limit, limit)));
        var result = new NeuralFoilPrediction(confidence, (first[1] - second[1]) / 4,
            Math.Exp((first[2] + second[2] - 4)), (first[3] - second[3]) / 40,
            Math.Clamp((first[4] + second[5]) / 2, 0, 1),
            Math.Clamp((first[5] + second[4]) / 2, 0, 1));
        if (!double.IsFinite(result.Cl) || !double.IsFinite(result.Cd) || !double.IsFinite(result.Cm) ||
            !double.IsFinite(result.AnalysisConfidence))
            throw new ContractError("ANA-POLAR-NONFINITE", "NeuralFoil returned a non-finite prediction.");
        return result;
    }

    private void Forward(ReadOnlySpan<double> input, Span<double> output)
    {
        Span<double> current = stackalloc double[512];
        Span<double> next = stackalloc double[512];
        for (int layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            Layer layer = layers[layerIndex];
            ReadOnlySpan<double> source = layerIndex == 0 ? input : current[..layer.Columns];
            Span<double> target = layerIndex == layers.Length - 1 ? output : next[..layer.Rows];
            for (int row = 0; row < layer.Rows; row++)
            {
                double sum = layer.Bias[row];
                int offset = row * layer.Columns;
                for (int col = 0; col < layer.Columns; col++) sum += layer.Matrix[offset + col] * source[col];
                target[row] = layerIndex == layers.Length - 1 ? sum : sum / (1 + Math.Exp(-sum));
            }
            if (layerIndex != layers.Length - 1) next[..layer.Rows].CopyTo(current);
        }
    }

    private double Mahalanobis(ReadOnlySpan<double> input)
    {
        double sum = 0;
        for (int col = 0; col < 25; col++)
        {
            double product = 0;
            for (int row = 0; row < 25; row++)
                product += (input[row] - mean[row]) * inverseCovariance[row * 25 + col];
            sum += product * (input[col] - mean[col]);
        }
        return sum;
    }
}
