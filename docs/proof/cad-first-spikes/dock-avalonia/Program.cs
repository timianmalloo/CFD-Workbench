using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Dock.Avalonia.Controls;
using Dock.Avalonia.Themes.Fluent;
using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;
using Dock.Serializer.SystemTextJson;
using Dock.Settings;

static class Program {
  [STAThread]
  public static int Main(string[] args) {
    if (args.Length > 0 && args[0] == "reflect") { Reflect.Run(); return 0; }
    if (args.Length > 0 && args[0] == "managed") DockSettings.UseManagedWindows = true;
    return AppBuilder.Configure<App>().UsePlatformDetect().StartWithClassicDesktopLifetime(args);
  }
}

static class Reflect {
  static Type? Find(string full) => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(full)).FirstOrDefault(t => t != null);
  static void Chain(Type t) { var c = t; var s = new List<string>(); while (c != null) { s.Add(c.FullName!); c = c.BaseType; } Console.WriteLine("CHAIN " + string.Join(" -> ", s)); }
  static void Members(string full) { var t = Find(full); if (t == null) { Console.WriteLine("MISSING " + full); return; }
    var m = t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly).Where(x => x is not MethodInfo mi || !mi.IsSpecialName).Select(x => x.MemberType.ToString()[0] + ":" + x.Name).Distinct();
    Console.WriteLine("MEMBERS " + t.Assembly.GetName().Name + " " + full + " = " + string.Join(" ", m)); }
  public static void Run() {
    _ = typeof(Window); _ = typeof(AvaloniaObject); _ = typeof(KeyGesture); _ = typeof(DockSerializer); Console.WriteLine("SCREENS_TYPE " + typeof(TopLevel).GetProperty("Screens")!.PropertyType.FullName);
    Chain(typeof(HostWindow)); Chain(typeof(ManagedHostWindow)); Chain(typeof(NativeMenuBar)); Chain(typeof(NativeMenu)); Chain(typeof(NativeMenuItem));
    foreach (var n in new[] { "Avalonia.Controls.Screens", "Avalonia.Platform.Screen", "Avalonia.Controls.NativeMenu", "Avalonia.Controls.NativeMenuItem", "Avalonia.Controls.NativeMenuItemBase", "Avalonia.Controls.NativeMenuItemSeparator", "Avalonia.Controls.NativeMenuBar", "Avalonia.Input.Platform.PlatformHotkeyConfiguration", "Avalonia.Platform.IPlatformSettings", "Avalonia.Controls.TopLevel", "Avalonia.Application", "Dock.Serializer.SystemTextJson.DockSerializer", "Dock.Model.DockState", "Dock.Model.DockWorkspaceManager" }) Members(n);
    Console.WriteLine($"DOCKSETTINGS UseManagedWindows={DockSettings.UseManagedWindows} FloatingWindowHostMode={DockSettings.FloatingWindowHostMode} CloseFloatingOnMainClose={DockSettings.CloseFloatingWindowsOnMainWindowClose}");
  }
}

class App : Application {
  public override void Initialize() {
    Styles.Add(new FluentTheme());
    Styles.Add(new DockFluentTheme());
    RequestedThemeVariant = ThemeVariant.Light;
    var appMenu = new NativeMenu();
    appMenu.Add(new NativeMenuItem("About DockProbe"));
    NativeMenu.SetMenu(this, appMenu);
  }
  public override void OnFrameworkInitializationCompleted() {
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d) d.MainWindow = new MainWindow();
    base.OnFrameworkInitializationCompleted();
  }
}

class ProbeFactory : Factory {
  public override IRootDock CreateLayout() {
    var props = new Tool { Id = "Properties", Title = "Properties" };
    var browser = new Tool { Id = "Browser", Title = "Browser" };
    var points = new Tool { Id = "Points", Title = "Points" };
    var messages = new Tool { Id = "Messages", Title = "Messages" };
    var model = new Document { Id = "Model", Title = "Model area" };
    var left = new ToolDock { Id = "LeftDock", Proportion = 0.2, Alignment = Alignment.Left, ActiveDockable = props, VisibleDockables = CreateList<IDockable>(props, browser) };
    var right = new ToolDock { Id = "RightDock", Proportion = 0.2, Alignment = Alignment.Right, ActiveDockable = points, VisibleDockables = CreateList<IDockable>(points) };
    var bottom = new ToolDock { Id = "BottomDock", Proportion = 0.25, Alignment = Alignment.Bottom, ActiveDockable = messages, VisibleDockables = CreateList<IDockable>(messages) };
    var docs = new DocumentDock { Id = "Docs", ActiveDockable = model, VisibleDockables = CreateList<IDockable>(model) };
    var top = new ProportionalDock { Id = "Top", Orientation = Orientation.Horizontal, VisibleDockables = CreateList<IDockable>(left, new ProportionalDockSplitter(), docs, new ProportionalDockSplitter(), right) };
    var main = new ProportionalDock { Id = "Main", Orientation = Orientation.Vertical, ActiveDockable = top, VisibleDockables = CreateList<IDockable>(top, new ProportionalDockSplitter(), bottom) };
    var root = CreateRootDock(); root.Id = "Root"; root.IsCollapsable = false; root.ActiveDockable = main; root.DefaultDockable = main; root.VisibleDockables = CreateList<IDockable>(main);
    return root;
  }
}

class MainWindow : Window {
  readonly ProbeFactory _f = new();
  readonly IRootDock _layout;
  public MainWindow() {
    Title = "DockProbe Main"; Width = 1000; Height = 700;
    var mods = Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control;
    var menu = new NativeMenu();
    var fileMenu = new NativeMenu();
    var save = new NativeMenuItem("Save layout") { Gesture = new KeyGesture(Key.S, mods) };
    save.Click += (_, _) => Console.WriteLine("save clicked");
    fileMenu.Add(save); fileMenu.Add(new NativeMenuItemSeparator()); fileMenu.Add(new NativeMenuItem("Float Properties") { Gesture = new KeyGesture(Key.F, mods | KeyModifiers.Shift) });
    menu.Add(new NativeMenuItem("File") { Menu = fileMenu });
    NativeMenu.SetMenu(this, menu);
    _layout = _f.CreateLayout(); _f.InitLayout(_layout);
    var dock = new DockControl { Factory = _f, Layout = _layout, InitializeFactory = true };
    var panel = new DockPanel();
    var bar = new NativeMenuBar(); DockPanel.SetDock(bar, global::Avalonia.Controls.Dock.Top);
    panel.Children.Add(bar); panel.Children.Add(dock);
    Content = panel;
    Opened += async (_, _) => {
      try { await Probe(); } catch (Exception e) { Console.WriteLine("PROBE_ERROR " + e); }
      await Task.Delay(6000);
      ((IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!).Shutdown();
    };
  }
  async Task Probe() {
    await Task.Delay(1500);
    var hk = PlatformSettings?.HotkeyConfiguration;
    Console.WriteLine($"HOTKEY CommandModifiers={hk?.CommandModifiers} Copy=[{string.Join(",", (IEnumerable<KeyGesture>?)hk?.Copy ?? Array.Empty<KeyGesture>())}]");
    Console.WriteLine($"NATIVEMENU exported(main)={NativeMenu.GetIsNativeMenuExported(this)}");
    foreach (var s in Screens.All) Console.WriteLine($"SCREEN name={s.DisplayName} primary={s.IsPrimary} bounds={s.Bounds} working={s.WorkingArea} scaling={s.Scaling}");
    var props = _f.Find(d => d.Id == "Properties").First();
    _f.FloatDockable(props);
    await Task.Delay(1500);
    Console.WriteLine($"ROOT.Windows={_layout.Windows?.Count} HostWindows={_f.HostWindows.Count}");
    foreach (var w in _layout.Windows ?? new List<IDockWindow>()) Console.WriteLine($"DOCKWINDOW id={w.Id} x={w.X} y={w.Y} w={w.Width} h={w.Height} state={w.WindowState} host={w.Host?.GetType().FullName}");
    var life = (IClassicDesktopStyleApplicationLifetime)Application.Current!.ApplicationLifetime!;
    foreach (var win in life.Windows) { var h = win.TryGetPlatformHandle(); Console.WriteLine($"AVWINDOW type={win.GetType().FullName} title={win.Title} pos={win.Position} size={win.Bounds.Size} handle={h?.HandleDescriptor}:{h?.Handle} screen={Screens.ScreenFromPoint(win.Position)?.DisplayName}"); }
    TrySer("stj-IDock", () => new Dock.Serializer.SystemTextJson.DockSerializer(), s => s.Serialize<IDock>(_layout), (s, j) => s.Deserialize<IDock>(j));
    TrySer("stj-RootDock", () => new Dock.Serializer.SystemTextJson.DockSerializer(), s => s.Serialize((RootDock)_layout), (s, j) => s.Deserialize<RootDock>(j));
    TrySer("newtonsoft-RootDock", () => new Dock.Serializer.DockSerializer(), s => s.Serialize((RootDock)_layout), (s, j) => s.Deserialize<RootDock>(j));
    Console.WriteLine("READY_FOR_EXTERNAL_COUNT");
  }
  void TrySer(string label, Func<IDockSerializer> mk, Func<IDockSerializer, string> ser, Func<IDockSerializer, string, IDock?> de) {
    try {
      var s = mk(); var json = ser(s); File.WriteAllText($"layout-{label}.json", json);
      var back = de(s, json) as IRootDock;
      var w = back?.Windows?.FirstOrDefault();
      Console.WriteLine($"SER {label} OK len={json.Length} backType={back?.GetType().Name} windows={back?.Windows?.Count} win0=({w?.X},{w?.Y},{w?.Width},{w?.Height}) ids=[{string.Join(",", Ids(back))}]");
      if (back != null) { _f.InitLayout(back); Console.WriteLine($"SER {label} InitLayout OK; Properties owner={_f.Find(back, d => d.Id == "Properties").FirstOrDefault()?.Owner?.Id}"); }
    } catch (Exception e) { Console.WriteLine($"SER {label} FAIL {e.GetType().Name}: {e.Message.Split(Environment.NewLine)[0]}"); }
  }
  static IEnumerable<string> Ids(IDockable? d) {
    if (d == null) yield break;
    if (d is Dock.Model.Core.IDock k) { foreach (var c in k.VisibleDockables ?? new List<IDockable>()) foreach (var i in Ids(c)) yield return i; }
    else yield return d.Id;
    if (d is IRootDock r) foreach (var w in r.Windows ?? new List<IDockWindow>()) foreach (var i in Ids(w.Layout)) yield return "float:" + i;
  }
}
