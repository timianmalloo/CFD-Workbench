using CfdWorkbench.Core;

namespace CfdWorkbench.Core.Tests;

// The third scalar of the one placement rule (design 3D elevations §13 OI-11): it records each operation as a
// fully parenthesised token instead of computing. Same PlacementRule code, not a third implementation.
internal readonly record struct Trace(string Text) : IPlacementScalar<Trace>
{
    public static Trace Leaf(string name) => new(name);
    public static Trace operator +(Trace left, Trace right) => new("(" + left.Text + "+" + right.Text + ")");
    public static Trace operator -(Trace left, Trace right) => new("(" + left.Text + "-" + right.Text + ")");
    public static Trace operator *(Trace left, Trace right) => new("(" + left.Text + "*" + right.Text + ")");
    public static Trace Half => new("half");
    public static Trace Point(double value) => new(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
    public static Trace BlendWeight(double eta, double left, double right) => new("weight");
    public static Trace Radians(Trace degrees) => new("rad(" + degrees.Text + ")");
    public static (Trace Sin, Trace Cos) SinCos(Trace radians) => (new("sin(" + radians.Text + ")"), new("cos(" + radians.Text + ")"));
}
