// S8 probe: can Dock 11.3.12.1 capability overrides block split drops while still allowing tab (Fill) and float (Window)?
// Drives the library's own DockManager validation path (IsDockTargetVisible, ValidateDockable) with no UI,
// then executes the drop and prints the resulting tree, so "split happened" is observed, not inferred.
using System.Reflection;
using Dock.Model;
using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

static class Program {
  static readonly DockOperation[] Ops = { DockOperation.Fill, DockOperation.Left, DockOperation.Right, DockOperation.Top, DockOperation.Bottom, DockOperation.Window };

  // Each override set is applied to a fresh layout. src = tool C (in RightDock); tgtDock = LeftDock; tgtTool = tool A (in LeftDock).
  static readonly (string Name, Action<Layout> Apply)[] Sets = {
    ("baseline", _ => { }),
    ("tgtDock.CanDrop=false", l => l.TgtDock.CanDrop = false),
    ("tgtDock.DockCapabilityOverrides{CanDrop=false}", l => l.TgtDock.DockCapabilityOverrides = new DockCapabilityOverrides { CanDrop = false }),
    ("tgtDock.DockCapabilityPolicy{CanDrop=false}", l => l.TgtDock.DockCapabilityPolicy = new DockCapabilityPolicy { CanDrop = false }),
    ("root.RootDockCapabilityPolicy{CanDrop=false}", l => l.Root.RootDockCapabilityPolicy = new DockCapabilityPolicy { CanDrop = false }),
    ("tgtTool.CanDrop=false", l => l.TgtTool.CanDrop = false),
    ("tgtTool.DockCapabilityOverrides{CanDrop=false}", l => l.TgtTool.DockCapabilityOverrides = new DockCapabilityOverrides { CanDrop = false }),
    ("tgtDock.AllowedDropOperations=Fill|Window", l => ((IDockableDockingRestrictions)l.TgtDock).AllowedDropOperations = DockOperationMask.Fill | DockOperationMask.Window),
    ("tgtTool.AllowedDropOperations=Fill|Window", l => ((IDockableDockingRestrictions)l.TgtTool).AllowedDropOperations = DockOperationMask.Fill | DockOperationMask.Window),
    ("src.AllowedDockOperations=Fill|Window", l => ((IDockableDockingRestrictions)l.Src).AllowedDockOperations = DockOperationMask.Fill | DockOperationMask.Window),
  };

  public static int Main(string[] args) {
    Console.WriteLine($"DOCK_MODEL {typeof(DockManager).Assembly.GetName().Version} DOCK_MVVM {typeof(Factory).Assembly.GetName().Version}");
    Console.WriteLine("BASELINE_TREE " + Tree(Layout.Build().Root));
    foreach (var (name, apply) in Sets)
      foreach (var targetKind in new[] { "tgtDock", "tgtTool", "docsDock", "topDock", "root" })
        foreach (var op in Ops) Probe(name, apply, targetKind, op);
    ScanUiCallers();
    return 0;
  }

  static void Probe(string setName, Action<Layout> apply, string targetKind, DockOperation op) {
    // Validate-only on one fresh layout; execute on a second fresh layout so validation state cannot leak.
    var l = Layout.Build(); apply(l);
    var tgt = l.Target(targetKind);
    var mgr = new DockManager(new DockService());
    bool visible = mgr.IsDockTargetVisible(l.Src, tgt, op);
    bool valid = mgr.ValidateDockable(l.Src, tgt, DragAction.Move, op, bExecute: false);
    var eval = mgr.LastCapabilityEvaluation;
    var x = Layout.Build(); apply(x);
    var xt = x.Target(targetKind);
    var xm = new DockManager(new DockService());
    string exec, tree;
    try { exec = xm.ValidateDockable(x.Src, xt, DragAction.Move, op, bExecute: true).ToString(); tree = Tree(x.Root); }
    catch (Exception e) { exec = "THREW " + e.GetType().Name + ": " + e.Message.Split('\n')[0]; tree = Tree(x.Root); }
    int leftGroups = CountToolDocksUnder(x.Root, "Top");
    Console.WriteLine($"CASE set=[{setName}] target={targetKind} op={op} | IsDockTargetVisible={visible} ValidateDockable(execute:false)={valid} " +
      $"| LastCapabilityEvaluation={(eval is null ? "null" : $"{eval.Capability}:{eval.EffectiveValue}:{eval.EffectiveSource}")} " +
      $"| execute={exec} toolDocks={leftGroups} floatWindows={x.Root.Windows?.Count ?? 0} | TREE {tree}");
  }

  static int CountToolDocksUnder(IDockable d, string _) {
    int n = d is IToolDock td && td.VisibleDockables?.Count > 0 ? 1 : 0;
    if (d is IDock dk && dk.VisibleDockables != null) foreach (var c in dk.VisibleDockables) n += CountToolDocksUnder(c, _);
    return n;
  }

  static string Tree(IDockable d) {
    var kind = d switch { IRootDock => "Root", IProportionalDock p => "P" + (p.Orientation == Orientation.Horizontal ? "h" : "v"), IToolDock => "TD", IDocumentDock => "DD", IProportionalDockSplitter => "|", ITool => "T", IDocument => "D", _ => d.GetType().Name };
    if (d is IProportionalDockSplitter) return "|";
    var id = string.IsNullOrEmpty(d.Id) ? "" : ":" + d.Id;
    if (d is IDock dk && dk.VisibleDockables != null) return kind + id + "[" + string.Join(",", dk.VisibleDockables.Select(Tree)) + "]";
    return kind + id;
  }

  // Which Dock.Avalonia methods call the model's validation APIs? A byte scan of IL for call/callvirt/newobj tokens,
  // resolved through the module. Evidence that the UI drag path routes through the same DockManager calls.
  static void ScanUiCallers() {
    var asm = typeof(Dock.Avalonia.Controls.DockControl).Assembly;
    var wanted = new HashSet<string> { "ValidateDockable", "IsDockTargetVisible", "ValidateDock", "ValidateTool", "ValidateDocument", "Allows", "IsEnabled", "Evaluate" };
    const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    Console.WriteLine($"UISCAN assembly={asm.GetName().Name} {asm.GetName().Version} types={asm.GetTypes().Length}");
    int scanned = 0, resolved = 0;
    foreach (var t in asm.GetTypes())
      foreach (var m in t.GetMethods(all).Cast<MethodBase>().Concat(t.GetConstructors(all))) {
        byte[]? il; try { il = m.GetMethodBody()?.GetILAsByteArray(); } catch { continue; }
        if (il == null) continue;
        scanned++;
        var hits = new SortedSet<string>();
        for (int i = 0; i + 4 < il.Length; i++) {
          if (il[i] != 0x28 && il[i] != 0x6F) continue;
          int tok = BitConverter.ToInt32(il, i + 1);
          if ((tok >> 24) != 0x0A && (tok >> 24) != 0x06) continue;
          try {
            var callee = t.Module.ResolveMethod(tok, t.IsGenericType ? t.GetGenericArguments() : null, m.IsGenericMethod ? m.GetGenericArguments() : null);
            if (callee != null) resolved++;
            if (callee != null && wanted.Contains(callee.Name) && (callee.DeclaringType?.Namespace?.StartsWith("Dock.Model") ?? false))
              hits.Add(callee.DeclaringType!.Name + "." + callee.Name);
          } catch { }
        }
        if (hits.Count > 0) Console.WriteLine($"UICALL {t.FullName}.{m.Name} -> {string.Join(" ", hits)}");
      }
    Console.WriteLine($"UISCAN methodsWithIL={scanned} callTokensResolved={resolved}");
  }
}

sealed class Layout {
  public required IRootDock Root; public required IToolDock TgtDock; public required ITool TgtTool; public required ITool Src;
  public required IDocumentDock Docs; public required IProportionalDock Top;
  public IDockable Target(string k) => k switch { "tgtDock" => TgtDock, "tgtTool" => TgtTool, "docsDock" => Docs, "topDock" => Top, "root" => Root, _ => throw new ArgumentException(k) };
  public static Layout Build() {
    var f = new Factory();
    var a = new Tool { Id = "A", Title = "A" }; var b = new Tool { Id = "B", Title = "B" }; var c = new Tool { Id = "C", Title = "C" };
    var d = new Document { Id = "Doc", Title = "Doc" };
    var left = new ToolDock { Id = "LeftDock", Proportion = 0.25, Alignment = Alignment.Left, ActiveDockable = a, VisibleDockables = f.CreateList<IDockable>(a, b) };
    var right = new ToolDock { Id = "RightDock", Proportion = 0.25, Alignment = Alignment.Right, ActiveDockable = c, VisibleDockables = f.CreateList<IDockable>(c) };
    var docs = new DocumentDock { Id = "Docs", ActiveDockable = d, VisibleDockables = f.CreateList<IDockable>(d) };
    var top = new ProportionalDock { Id = "Top", Orientation = Orientation.Horizontal, VisibleDockables = f.CreateList<IDockable>(left, new ProportionalDockSplitter(), docs, new ProportionalDockSplitter(), right) };
    var root = f.CreateRootDock(); root.Id = "Root"; root.ActiveDockable = top; root.DefaultDockable = top; root.VisibleDockables = f.CreateList<IDockable>(top);
    f.InitLayout(root);
    return new Layout { Root = root, TgtDock = left, TgtTool = a, Src = c, Docs = docs, Top = top };
  }
}
