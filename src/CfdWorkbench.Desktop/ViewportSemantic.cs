namespace CfdWorkbench.Desktop;

/// <summary>One accessible target of the section canvas: its id, its name and its status (editable, fixed).</summary>
public sealed record ViewportSemantic(string Id, string Name, string Status);
