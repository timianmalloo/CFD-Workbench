namespace CfdWorkbench.Core;

/// <summary>A stable refusal at a document contract boundary.</summary>
public sealed class ContractError(string code) : Exception(code)
{
    public string Code { get; } = code;
}

internal static class Guard
{
    internal static void Require(bool condition, string code)
    {
        if (!condition) throw new ContractError(code);
    }
}
