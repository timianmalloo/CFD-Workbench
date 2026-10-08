using System;

static string Bits(double value) =>
    unchecked((ulong)BitConverter.DoubleToInt64Bits(value)).ToString("x16");

for (var i = 0; i <= 80; i++)
{
    var x = i / 80.0;
    Console.WriteLine(
        $"i={i:D2} Math.Cos(Math.PI*i/80.0)={Bits(Math.Cos(Math.PI * i / 80.0))} " +
        $"double.CosPi(i/80.0)={Bits(double.CosPi(x))} " +
        $"double.SinPi(i/80.0)={Bits(double.SinPi(x))} " +
        $"Math.Sin(Math.PI*i/80.0)={Bits(Math.Sin(Math.PI * i / 80.0))}");
}

Console.WriteLine($"Math.Atan(0.18)={Bits(Math.Atan(0.18))}");
Console.WriteLine($"Math.Atan2(0.06,0.94)={Bits(Math.Atan2(0.06, 0.94))}");
Console.WriteLine($"Math.Sin(0.1)={Bits(Math.Sin(0.1))}");
Console.WriteLine($"Math.Cos(0.1)={Bits(Math.Cos(0.1))}");
