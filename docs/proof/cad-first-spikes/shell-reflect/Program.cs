// Spike: reflect Dock 11.3.12.1 for the shell contract (drop restriction, float owner, hooks, window events).
using System.Reflection;
using Dock.Avalonia.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Settings;

static class Program
{
    static void Members(Type t, Func<MemberInfo, bool>? keep = null)
    {
        var m = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(x => x is not MethodInfo mi || !mi.IsSpecialName)
            .Where(x => keep is null || keep(x))
            .Select(x => x.MemberType.ToString()[0] + ":" + x.Name).Distinct().OrderBy(x => x);
        Console.WriteLine("MEMBERS " + t.FullName + " = " + string.Join(" ", m));
    }
    static Type? Find(string full) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(full)).FirstOrDefault(t => t != null);
    public static int Main()
    {
        Extra1.Run(); Extra.Run(); _ = typeof(HostWindow); _ = typeof(Factory); _ = typeof(DockSettings); _ = typeof(IDockable);
        Members(typeof(IDockable));
        Members(typeof(IDockWindow));
        Members(typeof(IFactory), x => x.Name.StartsWith("On") || x.Name.Contains("Float") || x.Name.Contains("Host") || x.Name.Contains("Owner") || x.Name.Contains("Drop") || x.Name.Contains("Can"));
        Members(typeof(DockSettings));
        Members(typeof(HostWindow), x => x.DeclaringType == typeof(HostWindow));
        foreach (var n in Enum.GetNames(typeof(DockOperation))) Console.Write("DockOperation." + n + " ");
        Console.WriteLine();
        foreach (var name in new[] { "Dock.Model.Core.DockMode", "Dock.Settings.DockFloatingWindowOwnerPolicy", "Dock.Model.Core.IDockManager", "Dock.Avalonia.Internal.DockManager", "Dock.Model.DockManager" })
        {
            var t = Find(name);
            if (t is null) { Console.WriteLine("MISSING " + name); continue; }
            if (t.IsEnum) Console.WriteLine("ENUM " + name + " = " + string.Join(",", Enum.GetNames(t))); else Members(t);
        }
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name!.StartsWith("Dock")))
            foreach (var t in a.GetExportedTypes().Where(t => t.Name.Contains("Owner") || t.Name.Contains("Validate") || t.Name.Contains("Locator")))
                Console.WriteLine("TYPE " + t.FullName);
        return 0;
    }
}
