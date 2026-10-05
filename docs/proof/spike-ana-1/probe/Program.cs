using System.Globalization;
using System.Diagnostics;

if (args.Length == 1 && args[0] == "--selftest")
{
    // Python NeuralFoil 0.3.2, xxxlarge, NACA 0012 CST, alpha=4, Re=1e6, Ncrit=4.
    double[] upper = [0.1728844, 0.15156292, 0.17376306, 0.12768079,
        0.16481846, 0.126376, 0.14589789, 0.13919961];
    double[] lower = upper.Select(x => -x).ToArray();
    double[] result = NeuralFoilProbe.Evaluate(upper, lower, 2.1347475352594767e-17,
        0.002547899733323931, 4, 1e6, 4);
    if (Math.Abs(result[1] - 0.4326830954416412) > 1e-6 ||
        Math.Abs(result[2] - 0.008423273060149472) > 1e-6)
        throw new Exception($"Python oracle mismatch: CL={result[1].ToString("G17", CultureInfo.InvariantCulture)} CD={result[2].ToString("G17", CultureInfo.InvariantCulture)}");
    Console.WriteLine("PASS PythonOracle_Naca0012_Alpha4_Re1e6_Ncrit4");
    return;
}

if (args.Length == 3 && args[0] == "--cases")
{
    var lines = File.ReadAllLines(args[1]);
    using var output = new StreamWriter(args[2]);
    output.WriteLine("case\t" + string.Join('\t', NeuralFoilProbe.OutputNames));
    foreach (string line in lines.Skip(1))
    {
        string[] fields = line.Split('\t');
        double Parse(int index) => double.Parse(fields[index], CultureInfo.InvariantCulture);
        double[] upper = Enumerable.Range(5, 8).Select(Parse).ToArray();
        double[] lower = Enumerable.Range(13, 8).Select(Parse).ToArray();
        double[] values = NeuralFoilProbe.Evaluate(upper, lower, Parse(21), Parse(22),
            Parse(2), Parse(3), Parse(4));
        output.WriteLine(fields[0] + "\t" + string.Join('\t', values.Select(value =>
            value.ToString("G17", CultureInfo.InvariantCulture))));
    }
    Console.WriteLine($"PASS ProbeCases {lines.Length - 1}");
    return;
}

if (args.Length == 2 && args[0] == "--benchmark")
{
    int count = int.Parse(args[1], CultureInfo.InvariantCulture);
    double[] upper = [0.1728844, 0.15156292, 0.17376306, 0.12768079,
        0.16481846, 0.126376, 0.14589789, 0.13919961];
    double[] lower = upper.Select(x => -x).ToArray();
    for (int i = 0; i < 20; i++)
        NeuralFoilProbe.Evaluate(upper, lower, 0, 0.002547899733323931, 4, 1e6, 4);
    var watch = Stopwatch.StartNew();
    double checksum = 0;
    for (int i = 0; i < count; i++)
        checksum += NeuralFoilProbe.Evaluate(upper, lower, 0, 0.002547899733323931, 4, 1e6, 4)[1];
    watch.Stop();
    Console.WriteLine($"count={count} total_ms={watch.Elapsed.TotalMilliseconds.ToString("G17", CultureInfo.InvariantCulture)} ms_per_call={(watch.Elapsed.TotalMilliseconds / count).ToString("G17", CultureInfo.InvariantCulture)} checksum={checksum.ToString("G17", CultureInfo.InvariantCulture)}");
    return;
}

Console.Error.WriteLine("Usage: probe --selftest | --cases INPUT OUTPUT | --benchmark COUNT");
Environment.ExitCode = 2;
