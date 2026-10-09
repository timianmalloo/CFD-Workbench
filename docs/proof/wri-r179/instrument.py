from pathlib import Path
import hashlib, json, tempfile

root = Path.cwd()
proof = root / 'docs/proof/wri-r179'
backup = Path(tempfile.gettempdir()) / 'wri-r179-originals'
backup.mkdir(exist_ok=True)
files = ['WorkbenchTests.cs', 'PropertiesCellsTests.cs', 'ControllerViewTests.cs']
records = []
for name in files:
    p = root / 'tests/CfdWorkbench.Desktop.Tests' / name
    raw = p.read_bytes()
    (backup / name).write_bytes(raw)
    records.append({'path': str(p.relative_to(root)).replace('\\', '/'), 'original_sha256': hashlib.sha256(raw).hexdigest()})

p = root / records[0]['path']
s = p.read_bytes().decode('utf-8').replace('\r\n', '\n')
helper = '''
static void R179Scale()
{
    var probe = new Window { Width = 300, Height = 200 };
    probe.Show();
    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
    Console.WriteLine(FormattableString.Invariant($"R179_SCALE_CONTEXT RenderScaling={probe.RenderScaling:R} PrimaryScaling={probe.Screens.Primary!.Scaling:R} WorkingArea={probe.Screens.Primary.WorkingArea} UseLayoutRounding={probe.UseLayoutRounding}"));
    probe.Close();
    Avalonia.Threading.Dispatcher.UIThread.RunJobs();
}
if (args.Contains("--r179-diagnostic", StringComparer.Ordinal))
{
    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();
    R179Scale();
    Environment.Exit(0);
}
'''
s = s.replace('bool themeEvidence =', helper + '\nbool themeEvidence =', 1)
for mode in ['shell-window', 'views', 'properties-cells', 'analysis']:
    anchor = f'if (args.Contains("--{mode}", StringComparer.Ordinal))\n{{\n    AppBuilder.Configure<App>().UsePlatformDetect().SetupWithoutStarting();'
    assert anchor in s, mode
    s = s.replace(anchor, anchor + '\n    R179Scale();', 1)
route = '    CfdWorkbench.Desktop.Tests.AnalysisPanelTests.Run();'
s = s.replace(route, route + '\n    if (Environment.GetEnvironmentVariable("CFD_TEST_ONLY") == "Analysis_FourViews_At1280x800_AndGeometryUnchangedAt1500x870")\n        CfdWorkbench.Desktop.Tests.AnalysisPanelTests.RunReadiness();', 1)
route = '    CfdWorkbench.Desktop.Tests.ControllerViewTests.Run();'
s = s.replace(route, route + '\n    if ((Environment.GetEnvironmentVariable("CFD_TEST_ONLY") ?? "").Split(\',\').Contains("ModelArea_Views_SeparatedByGutterAndFramed", StringComparer.Ordinal))\n        CfdWorkbench.Desktop.Tests.ControllerViewTests.RunReadiness();', 1)
p.write_bytes(s.encode('utf-8'))

p = root / records[2]['path']
s = p.read_bytes().decode('utf-8')
start = s.index('DesktopChecks.Check("ModelArea_ViewLabelDoubleClickOrReturn_OneViewAndBack"')
end = s.index('DesktopChecks.Check("DevicePixel_RoundedAndDeviceSampler_', start)
chunk = s[start:end]
chunk = chunk.replace('            var area = fixture.Area;', '            var area = fixture.Area;\n            Console.WriteLine(FormattableString.Invariant($"R179_P3 window={fixture.Window.Bounds} area={area.Bounds} RenderScaling={fixture.Window.RenderScaling:R}"));', 1)
chunk = chunk.replace('            area.ThreeDLabel.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));\n            fixture.Settle();', '            area.ThreeDLabel.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null!));\n            fixture.Settle();\n            Console.WriteLine(FormattableString.Invariant($"R179_P3 layout={fixture.Controller.Layout} PlanVisible={area.PlanSlot.IsEffectivelyVisible} ThreeDVisible={area.ThreeDSlot.IsEffectivelyVisible} ThreeDWidth={area.ThreeDSlot.Bounds.Width:R} PlanContentWidth={area.PlanContent.Bounds.Width:R} RoundedFrame={DevicePixel.Rounded(area.ThreeDSlot, 1):R}"));', 1)
chunk = chunk.replace('            area.FrontLabel.RaiseEvent', '            Console.WriteLine($"R179_P3 clauses second-double-click, Return-to-Side, picker-Back-to-Four completed; final-layout={fixture.Controller.Layout}");\n            area.FrontLabel.RaiseEvent', 1)
s = s[:start] + chunk + s[end:]
p.write_bytes(s.encode('utf-8'))

p = root / records[1]['path']
s = p.read_bytes().decode('utf-8')
anchor = '            double input = Right(presenter, presenter.TextLayout, 0, presenter.Text ?? "");'
assert anchor in s
extra = '''            foreach (var control in new Control[] { Need<TextBox>(host.Properties, "PointAftInput"), fact, presenter })
            {
                var origin = control.TranslatePoint(new Point(0, 0), host.Properties)!.Value;
                double scale = window.RenderScaling;
                Console.WriteLine(FormattableString.Invariant($"R179_ITEM6 name={control.Name ?? control.GetType().Name} BoundsDIP={control.Bounds} PropertiesOriginDIP={origin} WidthDIP={control.Bounds.Width:R} HeightDIP={control.Bounds.Height:R} PropertiesXDevice={origin.X * scale:R} PropertiesYDevice={origin.Y * scale:R} WidthDevice={control.Bounds.Width * scale:R} HeightDevice={control.Bounds.Height * scale:R} RenderScaling={scale:R}"));
            }
'''
s = s.replace(anchor, extra + anchor, 1)
p.write_bytes(s.encode('utf-8'))

for r in records:
    r['instrumented_sha256'] = hashlib.sha256((root / r['path']).read_bytes()).hexdigest()
(proof / 'source-hashes.json').write_text(json.dumps(records, indent=2), encoding='utf-8')
print('Temporary originals:', backup)
