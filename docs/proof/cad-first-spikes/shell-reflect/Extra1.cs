// Spike addendum 1: virtual-ness of DockManager validation, DockControl.DockManager setter, owner-mode enum.
using System.Reflection;
static class Extra1
{
    public static void Run()
    {
        var dm = typeof(Dock.Model.DockManager);
        foreach (var m in dm.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            Console.WriteLine($"DM {m.Name}({string.Join(",", m.GetParameters().Select(p => p.ParameterType.Name + " " + p.Name))}) virtual={m.IsVirtual && !m.IsFinal} sealedType={dm.IsSealed}");
        var dc = typeof(Dock.Avalonia.Controls.DockControl);
        foreach (var p in dc.GetProperties().Where(p => p.Name.Contains("Manager") || p.Name.Contains("Factory") || p.Name.Contains("Layout")))
            Console.WriteLine($"DC {p.Name}:{p.PropertyType.Name} set={p.SetMethod?.IsPublic}");
        Console.WriteLine("OWNERMODE " + string.Join(",", Enum.GetNames(typeof(Dock.Model.Core.DockWindowOwnerMode))));
        var caps = typeof(Dock.Model.Core.IDockable).GetProperty("DockCapabilityOverrides")!.PropertyType;
        Console.WriteLine("CAPS " + caps.FullName + " = " + string.Join(" ", caps.GetProperties().Select(p => p.Name)));
    }
}
