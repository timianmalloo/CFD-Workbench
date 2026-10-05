using System.Text;

// Throwaway reimplementation of neuralfoil/main.py 0.3.2 for numerical comparison.
internal static class NeuralFoilProbe
{
    private sealed record Layer(int Rows, int Columns, double[] Weights, double[] Bias);
    private sealed record Model(Layer[] Layers, double[] Mean, double[] InverseCovariance);
    private static readonly Model Weights = Load(Path.Combine(AppContext.BaseDirectory, "weights.bin"));

    public static string[] OutputNames { get; } = MakeOutputNames();

    private static string[] MakeOutputNames()
    {
        var names = new List<string> { "analysis_confidence", "CL", "CD", "CM", "Top_Xtr", "Bot_Xtr" };
        foreach (string group in new[] { "upper_bl_theta", "upper_bl_H", "upper_bl_ue/vinf",
                     "lower_bl_theta", "lower_bl_H", "lower_bl_ue/vinf" })
            for (int i = 0; i < 32; i++) names.Add($"{group}_{i}");
        return names.ToArray();
    }

    private static Model Load(string path)
    {
        using var reader = new BinaryReader(File.OpenRead(path), Encoding.ASCII);
        if (Encoding.ASCII.GetString(reader.ReadBytes(8)) != "NFPRB1\0\0")
            throw new InvalidDataException("Unexpected probe weight format");
        int count = reader.ReadInt32();
        if (count is < 1 or > 16) throw new InvalidDataException("Invalid layer count");
        var layers = new Layer[count];
        for (int l = 0; l < count; l++)
        {
            int rows = reader.ReadInt32(), columns = reader.ReadInt32();
            if (rows is < 1 or > 4096 || columns is < 1 or > 4096)
                throw new InvalidDataException("Invalid layer dimensions");
            var matrix = new double[rows * columns];
            var bias = new double[rows];
            for (int i = 0; i < matrix.Length; i++) matrix[i] = reader.ReadSingle();
            for (int i = 0; i < bias.Length; i++) bias[i] = reader.ReadSingle();
            layers[l] = new Layer(rows, columns, matrix, bias);
        }
        var mean = new double[25];
        var inverse = new double[25 * 25];
        for (int i = 0; i < 25; i++) mean[i] = reader.ReadSingle();
        for (int i = 0; i < inverse.Length; i++) inverse[i] = reader.ReadDouble();
        if (reader.BaseStream.Position != reader.BaseStream.Length)
            throw new InvalidDataException("Trailing bytes in weight file");
        if (layers[0].Columns != 25 || layers[^1].Rows != 198 ||
            layers.Skip(1).Where((layer, i) => layer.Columns != layers[i].Rows).Any())
            throw new InvalidDataException("Unexpected network shape");
        return new Model(layers, mean, inverse);
    }

    private static double[] Net(double[] input)
    {
        double[] x = input;
        for (int l = 0; l < Weights.Layers.Length; l++)
        {
            Layer layer = Weights.Layers[l];
            var y = new double[layer.Rows];
            for (int row = 0; row < layer.Rows; row++)
            {
                double sum = layer.Bias[row];
                int offset = row * layer.Columns;
                for (int col = 0; col < layer.Columns; col++)
                    sum += layer.Weights[offset + col] * x[col];
                y[row] = l == Weights.Layers.Length - 1 ? sum : sum / (1 + Math.Exp(-sum));
            }
            x = y;
        }
        return x;
    }

    private static double Mahalanobis(double[] input)
    {
        double sum = 0;
        for (int col = 0; col < 25; col++)
        {
            double product = 0;
            for (int row = 0; row < 25; row++)
                product += (input[row] - Weights.Mean[row]) * Weights.InverseCovariance[row * 25 + col];
            sum += product * (input[col] - Weights.Mean[col]);
        }
        return sum;
    }

    public static double[] Evaluate(double[] upper, double[] lower, double leadingEdge,
        double trailingEdge, double alpha, double reynolds, double ncrit)
    {
        if (upper.Length != 8 || lower.Length != 8 || reynolds <= 0)
            throw new ArgumentException("Expected eight CST weights per side and positive Reynolds number");
        var x = new double[25];
        Array.Copy(upper, 0, x, 0, 8);
        Array.Copy(lower, 0, x, 8, 8);
        double angle = alpha * Math.PI / 180;
        x[16] = leadingEdge;
        x[17] = trailingEdge * 50;
        x[18] = Math.Sin(2 * angle);
        x[19] = Math.Cos(angle);
        x[20] = 1 - x[19] * x[19];
        x[21] = (Math.Log(reynolds) - 12.5) / 3.5;
        x[22] = (ncrit - 9) / 4.5;
        x[23] = x[24] = 1;

        double[] y = Net(x);
        y[0] -= Mahalanobis(x) / 50;
        var flipped = (double[])x.Clone();
        for (int i = 0; i < 8; i++)
        {
            flipped[i] = -x[8 + i];
            flipped[8 + i] = -x[i];
        }
        flipped[16] = -x[16];
        flipped[18] = -x[18];
        flipped[23] = x[24];
        flipped[24] = x[23];
        double[] mirrored = Net(flipped);
        mirrored[0] -= Mahalanobis(flipped) / 50;

        var result = new double[198];
        for (int i = 0; i < result.Length; i++) result[i] = (y[i] + mirrored[i]) / 2;
        result[1] = (y[1] - mirrored[1]) / 2;
        result[3] = (y[3] - mirrored[3]) / 2;
        result[4] = (y[4] + mirrored[5]) / 2;
        result[5] = (y[5] + mirrored[4]) / 2;
        for (int i = 0; i < 32; i++)
        {
            result[6 + i] = (y[6 + i] + mirrored[6 + 96 + i]) / 2;
            result[6 + 32 + i] = (y[6 + 32 + i] + mirrored[6 + 128 + i]) / 2;
            result[6 + 64 + i] = (y[6 + 64 + i] - mirrored[6 + 160 + i]) / 2;
            result[6 + 96 + i] = (y[6 + 96 + i] + mirrored[6 + i]) / 2;
            result[6 + 128 + i] = (y[6 + 128 + i] + mirrored[6 + 32 + i]) / 2;
            result[6 + 160 + i] = (y[6 + 160 + i] - mirrored[6 + 64 + i]) / 2;
        }
        double lnEpsilon = Math.Log(10 / double.MaxValue);
        result[0] = 1 / (1 + Math.Exp(-Math.Clamp(result[0], lnEpsilon, -lnEpsilon)));
        result[1] /= 2;
        result[2] = Math.Exp((result[2] - 2) * 2);
        result[3] /= 20;
        result[4] = Math.Clamp(result[4], 0, 1);
        result[5] = Math.Clamp(result[5], 0, 1);
        for (int i = 0; i < 32; i++)
        {
            result[6 + i] = (Math.Pow(10, result[6 + i]) - 0.1) /
                (Math.Abs(result[6 + 64 + i]) * reynolds);
            result[6 + 32 + i] = 2.6 * Math.Exp(result[6 + 32 + i]);
            result[6 + 96 + i] = (Math.Pow(10, result[6 + 96 + i]) - 0.1) /
                (Math.Abs(result[6 + 160 + i]) * reynolds);
            result[6 + 128 + i] = 2.6 * Math.Exp(result[6 + 128 + i]);
        }
        return result;
    }
}
