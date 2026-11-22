using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using CfdWorkbench.Analysis;
using CfdWorkbench.Analysis.Export;
using CfdWorkbench.Core;
using CfdWorkbench.Desktop.Analysis;
using CfdWorkbench.Desktop.Shell;

namespace CfdWorkbench.Desktop.Tests;

/// <summary>
/// Real OS-level Windows UI Automation (UIAutomationCore.dll / IUIAutomation COM) tree probe (--uia-probe).
/// Attaches via IUIAutomation.ElementFromHandle to the real MainWindow HWND (and modal dialog HWNDs) on Windows,
/// skipping cleanly off Windows.
///
/// CRITICAL ANTI-DEADLOCK CONTRACT (Wave 7 C-5):
/// All IUIAutomation COM calls run on a dedicated MTA background thread (COINIT_MULTITHREADED) while the Avalonia
/// UI thread pumps Dispatcher.UIThread.RunJobs() so WM_GETOBJECT never STA-deadlocks.
/// </summary>
public static unsafe class WindowsUiaTreeTests
{
    public sealed record UiaNode(
        string Name,
        int ControlType,
        string AutomationId,
        int Left,
        int Top,
        int Right,
        int Bottom,
        bool IsKeyboardFocusable,
        bool IsEnabled,
        bool IsOffscreen)
    {
        public int Width => Right - Left;
        public int Height => Bottom - Top;
        public bool HasNonEmptyBounds => Width > 0 && Height > 0;
    }

    public sealed record UiaSnapshot(
        string Surface,
        IntPtr Hwnd,
        string RootName,
        int RootControlType,
        IReadOnlyList<UiaNode> Nodes,
        int PatternInvokeCount,
        int PatternValueCount,
        int PatternSelectionItemCount);

    public static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("SKIP WindowsUiaTree_OsComProbe skipped on non-Windows host");
            return;
        }

        DesktopChecks.Check("WindowsUiaTree_StartSetupCadSectionAnalysisSweepsAndModals_OsNativeTree", ProbeAllSurfaces);
    }

    private static void ProbeAllSurfaces()
    {
        var window = new MainWindow(null, macOS: false) { Width = 1280, Height = 800, RequestedThemeVariant = ThemeVariant.Light };
        try
        {
            window.Show();
            Settle(window);
            IntPtr mainHwnd = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (mainHwnd == IntPtr.Zero)
                throw new InvalidOperationException("MainWindow platform HWND unavailable");

            var host = (ShellHost)window.Content!;
            var controller = host.Controller;

            // 1. Start surface
            var start = CaptureOnMtaThread("Start", mainHwnd, window);
            AssertSurface(start, requiredNames: ["New foil", "New from example", "Open…"], minFocusable: 3);

            // 2. Setup surface (non-modal first navbar segment)
            controller.EnterSetup();
            host.RefreshPanes();
            Settle(window);
            var setup = CaptureOnMtaThread("Setup", mainHwnd, window);
            AssertSurface(setup, requiredNames: ["Setup brief and soft targets tab", "Switch to CAD workspace"], minFocusable: 3);

            // 3. CAD surface (open example foil)
            DesktopChecks.PumpToCompletion(controller.OpenExampleAsync(), "open example", TimeSpan.FromSeconds(15));
            controller.EnterCad();
            host.RefreshPanes();
            Settle(window);
            var cad = CaptureOnMtaThread("CAD", mainHwnd, window);
            AssertSurface(cad, requiredNames: ["Properties", "Browser"], minFocusable: 5);

            // 4. Section Editor surface
            var station = controller.Inspection!.Authored.Assignments[0];
            controller.Select(new Selection.Station(0, station.Eta));
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section", TimeSpan.FromSeconds(15));
            host.RefreshPanes();
            Settle(window);
            var sectionEditor = CaptureOnMtaThread("SectionEditor", mainHwnd, window);
            AssertSurface(sectionEditor, requiredNames: ["Cancel", "Finish section"], minFocusable: 3);
            controller.CancelSection();
            host.RefreshPanes();
            Settle(window);

            // 5. Analysis surface (ConditionsBand + AnalysisPanel + Compare + Layers)
            controller.ToggleAnalysis();
            host.RefreshPanes();
            Settle(window);
            var analysis = CaptureOnMtaThread("Analysis", mainHwnd, window);
            AssertSurface(analysis, requiredNames: ["Evaluate", "Find operating alpha", "Spanwise loading", "Compare"], minFocusable: 5);

            // 6. Sweeps surface
            controller.EnterSweeps();
            host.RefreshPanes();
            Settle(window);
            var sweeps = CaptureOnMtaThread("Sweeps", mainHwnd, window);
            AssertSurface(sweeps, requiredNames: ["Experiment setup sub-tab", "Local Run sub-tab", "Sweep Results sub-tab"], minFocusable: 3);

            // 7. Modal dialogs (real modal HWNDs: FindAlphaDialog & ExportDialog & CatalogDialog)
            controller.ToggleAnalysis();
            host.RefreshPanes();
            Settle(window);

            var findDialog = new FindAlphaDialog(controller, 5.14, _ => { }, controller.AnalysisUnits);
            try
            {
                findDialog.Show(window);
                Settle(findDialog);
                IntPtr findHwnd = findDialog.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                var findSnap = CaptureOnMtaThread("Modal.FindAlpha", findHwnd, window, findDialog);
                AssertSurface(findSnap, requiredNames: ["Target CL", "Find α", "Cancel"], minFocusable: 4);
            }
            finally { findDialog.Close(); Settle(window); }

            var exportSession = new ExportSession(controller.ExportSnapshot()!);
            var exportDialog = new ExportDialog(exportSession);
            try
            {
                exportDialog.Show(window);
                Settle(exportDialog);
                IntPtr exportHwnd = exportDialog.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                var exportSnap = CaptureOnMtaThread("Modal.Export", exportHwnd, window, exportDialog);
                AssertSurface(exportSnap, requiredNames: ["Cancel"], minFocusable: 4);
            }
            finally { exportDialog.Close(); Settle(window); }

            controller.EnterCad();
            DesktopChecks.PumpToCompletion(controller.EnterSectionAsync(0, EntryOrigin.Palette), "enter section for catalog", TimeSpan.FromSeconds(15));
            var catalogDialog = new CatalogDialog(controller, controller.OpenCatalog());
            try
            {
                catalogDialog.Show(window);
                Settle(catalogDialog);
                IntPtr catalogHwnd = catalogDialog.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
                var catalogSnap = CaptureOnMtaThread("Modal.Catalog", catalogHwnd, window, catalogDialog);
                AssertSurface(catalogSnap, requiredNames: ["Cancel"], minFocusable: 2);
            }
            finally { catalogDialog.Close(); controller.CancelSection(); Settle(window); }

            Console.WriteLine("UIA-PROBE-SUMMARY surfaces=9 bridge=UIAutomationCore.dll thread=MTA status=PASS");
        }
        finally
        {
            typeof(MainWindow).GetField("closeApproved", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(window, true);
            window.Close();
        }
    }

    private static void AssertSurface(UiaSnapshot snap, string[] requiredNames, int minFocusable)
    {
        if (snap.RootControlType != UIA_WindowControlTypeId)
            throw new InvalidOperationException($"UIA {snap.Surface}: root ControlType expected Window ({UIA_WindowControlTypeId}), got {snap.RootControlType}");
        if (snap.Nodes.Count < 4)
            throw new InvalidOperationException($"UIA {snap.Surface}: tree unexpectedly shallow ({snap.Nodes.Count} nodes)");

        int focusableWithBounds = snap.Nodes.Count(n => n.IsKeyboardFocusable && n.HasNonEmptyBounds);
        int namedCount = snap.Nodes.Count(n => !string.IsNullOrWhiteSpace(n.Name));
        int withBoundsCount = snap.Nodes.Count(n => n.HasNonEmptyBounds);

        Console.WriteLine(FormattableString.Invariant(
            $"UIA-PROBE surface={snap.Surface} nodes={snap.Nodes.Count} named={namedCount} bounded={withBoundsCount} focusableBounded={focusableWithBounds} invoke={snap.PatternInvokeCount} value={snap.PatternValueCount} selItem={snap.PatternSelectionItemCount}"));

        if (focusableWithBounds < minFocusable)
            throw new InvalidOperationException($"UIA {snap.Surface}: only {focusableWithBounds} keyboard-focusable nodes with non-empty BoundingRectangle (expected >= {minFocusable})");

        foreach (string req in requiredNames)
        {
            bool found = snap.Nodes.Any(n =>
                n.Name.Contains(req, StringComparison.OrdinalIgnoreCase) &&
                n.ControlType != 0 &&
                (n.HasNonEmptyBounds || !n.IsOffscreen));
            if (!found)
            {
                string sample = string.Join(", ", snap.Nodes.Where(n => !string.IsNullOrWhiteSpace(n.Name)).Select(n => $"{n.Name}[{n.ControlType}]").Distinct().Take(25));
                throw new InvalidOperationException($"UIA {snap.Surface}: expected OS UIA node containing '{req}' not found. Sample: {sample}");
            }
        }
    }

    /// <summary>
    /// Runs IUIAutomation COM walk on a dedicated MTA thread while pumping Avalonia's STA UI thread so WM_GETOBJECT never deadlocks.
    /// </summary>
    private static UiaSnapshot CaptureOnMtaThread(string surface, IntPtr hwnd, params Window[] windowsToPump)
    {
        if (hwnd == IntPtr.Zero)
            throw new InvalidOperationException($"UIA {surface}: target HWND is zero");

        UiaSnapshot? result = null;
        Exception? failure = null;
        using var done = new ManualResetEventSlim(false);
        var frame = new DispatcherFrame();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) => { timer.Stop(); frame.Continue = false; };
        timer.Start();

        var mta = new Thread(() =>
        {
            int hrInit = CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
            bool uninit = hrInit is 0 or 1; // S_OK or S_FALSE
            try
            {
                result = WalkHwndNative(surface, hwnd);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                if (uninit) CoUninitialize();
                done.Set();
                Dispatcher.UIThread.Post(() => { timer.Stop(); frame.Continue = false; }, DispatcherPriority.Send);
            }
        })
        {
            IsBackground = true,
            Name = "UiaMtaProbe-" + surface
        };
        if (OperatingSystem.IsWindows()) mta.SetApartmentState(ApartmentState.MTA);
        mta.Start();

        // PushFrame pumps both Win32 messages (WM_GETOBJECT from UIAutomationCore) and Avalonia dispatcher jobs.
        Dispatcher.UIThread.PushFrame(frame);
        timer.Stop();
        foreach (var w in windowsToPump) w.UpdateLayout();

        if (!done.IsSet)
            throw new TimeoutException($"UIA {surface}: MTA COM probe timed out after 15 s (potential WM_GETOBJECT deadlock)");
        if (failure is not null)
            throw new InvalidOperationException($"UIA {surface}: MTA COM probe failed: {failure.Message}", failure);
        return result ?? throw new InvalidOperationException($"UIA {surface}: MTA COM probe returned null");
    }

    private static UiaSnapshot WalkHwndNative(string surface, IntPtr hwnd)
    {
        Guid clsid = CLSID_CUIAutomation;
        Guid iid = IID_IUIAutomation;
        int hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_INPROC_SERVER, ref iid, out void* uia);
        if (hr < 0 || uia is null)
            throw new InvalidOperationException($"CoCreateInstance(CUIAutomation) failed: 0x{hr:X8}");

        void* walker = null;
        void* root = null;
        try
        {
            // IUIAutomation::get_ControlViewWalker (slot 14)
            hr = ((delegate* unmanaged[Stdcall]<void*, void**, int>)VTable(uia)[14])(uia, &walker);
            if (hr < 0 || walker is null)
                throw new InvalidOperationException($"get_ControlViewWalker failed: 0x{hr:X8}");

            // IUIAutomation::ElementFromHandle (slot 6)
            hr = ((delegate* unmanaged[Stdcall]<void*, IntPtr, void**, int>)VTable(uia)[6])(uia, hwnd, &root);
            if (hr < 0 || root is null)
                throw new InvalidOperationException($"ElementFromHandle failed: 0x{hr:X8}");

            var rootNode = ReadNode(root);
            var nodes = new List<UiaNode>();
            int invokeCount = 0, valueCount = 0, selItemCount = 0;

            void Visit(void* w, void* elem, int depth)
            {
                if (elem is null || depth > 24 || nodes.Count >= 600) return;
                nodes.Add(ReadNode(elem));
                if (SupportsPattern(elem, UIA_InvokePatternId)) invokeCount++;
                if (SupportsPattern(elem, UIA_ValuePatternId)) valueCount++;
                if (SupportsPattern(elem, UIA_SelectionItemPatternId)) selItemCount++;

                void* child = null;
                // IUIAutomationTreeWalker::GetFirstChildElement (slot 4)
                if (((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)VTable(w)[4])(w, elem, &child) >= 0 && child is not null)
                {
                    while (child is not null && nodes.Count < 600)
                    {
                        Visit(w, child, depth + 1);
                        void* next = null;
                        // IUIAutomationTreeWalker::GetNextSiblingElement (slot 6)
                        int hrNext = ((delegate* unmanaged[Stdcall]<void*, void*, void**, int>)VTable(w)[6])(w, child, &next);
                        Release(child);
                        child = hrNext >= 0 ? next : null;
                    }
                    if (child is not null) Release(child);
                }
            }

            Visit(walker, root, 0);
            return new UiaSnapshot(surface, hwnd, rootNode.Name, rootNode.ControlType, nodes, invokeCount, valueCount, selItemCount);
        }
        finally
        {
            if (root is not null) Release(root);
            if (walker is not null) Release(walker);
            Release(uia);
        }
    }

    private static UiaNode ReadNode(void* elem)
    {
        void** vt = VTable(elem);
        int controlType = 0;
        ((delegate* unmanaged[Stdcall]<void*, int*, int>)vt[21])(elem, &controlType);

        string name = ReadBstr(elem, vt[23]);
        string automationId = ReadBstr(elem, vt[29]);

        int focusable = 0, enabled = 0, offscreen = 0;
        ((delegate* unmanaged[Stdcall]<void*, int*, int>)vt[27])(elem, &focusable);
        ((delegate* unmanaged[Stdcall]<void*, int*, int>)vt[28])(elem, &enabled);
        ((delegate* unmanaged[Stdcall]<void*, int*, int>)vt[38])(elem, &offscreen);

        RECT rect = default;
        ((delegate* unmanaged[Stdcall]<void*, RECT*, int>)vt[43])(elem, &rect);

        return new UiaNode(name, controlType, automationId, rect.Left, rect.Top, rect.Right, rect.Bottom,
            focusable != 0, enabled != 0, offscreen != 0);
    }

    private static bool SupportsPattern(void* elem, int patternId)
    {
        void* pattern = null;
        // IUIAutomationElement::GetCurrentPattern (slot 16)
        int hr = ((delegate* unmanaged[Stdcall]<void*, int, void**, int>)VTable(elem)[16])(elem, patternId, &pattern);
        if (hr >= 0 && pattern is not null)
        {
            Release(pattern);
            return true;
        }
        return false;
    }

    private static string ReadBstr(void* elem, void* fnPtr)
    {
        IntPtr bstr = IntPtr.Zero;
        int hr = ((delegate* unmanaged[Stdcall]<void*, IntPtr*, int>)fnPtr)(elem, &bstr);
        if (hr < 0 || bstr == IntPtr.Zero) return "";
        try { return Marshal.PtrToStringBSTR(bstr) ?? ""; }
        finally { Marshal.FreeBSTR(bstr); }
    }

    private static void** VTable(void* comObj) => *(void***)comObj;

    private static void Release(void* comObj) =>
        ((delegate* unmanaged[Stdcall]<void*, uint>)VTable(comObj)[2])(comObj);

    private static void Settle(Window window)
    {
        for (int i = 0; i < 8; i++)
        {
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
        }
    }

    private static readonly Guid CLSID_CUIAutomation = new("FF48DBA4-60EF-4201-AA87-54103EEF594E");
    private static readonly Guid IID_IUIAutomation = new("30CBE57D-D9D0-452A-AB13-7AC5AC4825EE");
    private const uint CLSCTX_INPROC_SERVER = 1;
    private const uint COINIT_MULTITHREADED = 0;

    private const int UIA_WindowControlTypeId = 50032;
    private const int UIA_InvokePatternId = 10000;
    private const int UIA_ValuePatternId = 10002;
    private const int UIA_SelectionItemPatternId = 10010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    [DllImport("ole32.dll")]
    private static extern int CoCreateInstance(ref Guid rclsid, IntPtr pUnkOuter, uint dwClsContext, ref Guid riid, out void* ppv);
}
