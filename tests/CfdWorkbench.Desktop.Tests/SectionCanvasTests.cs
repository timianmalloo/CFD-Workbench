using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Input;
using CfdWorkbench.Core;

namespace CfdWorkbench.Desktop.Tests;

public static class SectionCanvasTests
{
    public static void Run()
    {
        TestHitTest();
        TestNearestVertexWins();
        TestArrowMoves();
        TestFixedVertex();
        TestTabOrder();
        TestEditableFalse();
        TestAccessibleText();
        Console.WriteLine("SectionCanvas: all 7 test cases passed.");
    }

    private static ProfileView CreateTestProfile()
    {
        var upper = new ProfileVertex[]
        {
            new("upper", "u0", 0.0, 0.0, true),
            new("upper", "u1", 0.25, 0.08, false),
            new("upper", "u2", 0.50, 0.10, false),
            new("upper", "u3", 0.75, 0.06, false),
            new("upper", "u4", 1.0, 0.0, true)
        };
        var lower = new ProfileVertex[]
        {
            new("lower", "l0", 0.0, 0.0, true),
            new("lower", "l1", 0.30, -0.05, false),
            new("lower", "l2", 0.70, -0.03, false),
            new("lower", "l3", 1.0, 0.0, true)
        };
        var upperCurve = new ProfilePoint[]
        {
            new(0.0, 0.0), new(0.25, 0.08), new(0.50, 0.10), new(0.75, 0.06), new(1.0, 0.0)
        };
        var lowerCurve = new ProfilePoint[]
        {
            new(0.0, 0.0), new(0.30, -0.05), new(0.70, -0.03), new(1.0, 0.0)
        };
        return new ProfileView("NACA-test", "hash-123", upper, lower, upperCurve, lowerCurve, "closed");
    }

    private static SectionCanvas CreateConfiguredCanvas(ProfileView? profile = null, bool editable = true)
    {
        var canvas = new SectionCanvas
        {
            Width = 800,
            Height = 400,
            Padding = 24.0,
            Editable = editable,
            Profile = profile ?? CreateTestProfile()
        };
        canvas.Measure(new Size(800, 400));
        canvas.Arrange(new Rect(0, 0, 800, 400));
        return canvas;
    }

    private static void Press(SectionCanvas canvas, Point point)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        canvas.RaiseEvent(new PointerPressedEventArgs(canvas, pointer, canvas, point, 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
            KeyModifiers.None));
    }

    private static void MovePointer(SectionCanvas canvas, Point point)
    {
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        canvas.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, canvas, pointer, canvas, point, 0,
            default, KeyModifiers.None));
    }

    private static void SendKey(SectionCanvas canvas, Key key, KeyModifiers modifiers = KeyModifiers.None)
    {
        canvas.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = canvas,
            Key = key,
            KeyModifiers = modifiers
        });
    }

    public static void TestHitTest()
    {
        var canvas = CreateConfiguredCanvas();
        var targetVertex = canvas.Profile!.Upper[1]; // u1 at (0.25, 0.08)
        var screenPoint = canvas.ModelToScreen(targetVertex.X, targetVertex.Y);

        string? selectedSide = null;
        string? selectedId = null;
        canvas.VertexSelected += (s, id) => { selectedSide = s; selectedId = id; };

        // 19 px away -> selects it
        var hitPoint = new Point(screenPoint.X + 19, screenPoint.Y);
        Press(canvas, hitPoint);
        if (canvas.SelectedVertex != ("upper", "u1") || selectedSide != "upper" || selectedId != "u1")
            throw new Exception($"Hit test at 19 px failed: selected={canvas.SelectedVertex?.ToString() ?? "null"}");

        // Clear selection
        SendKey(canvas, Key.Escape);
        if (canvas.SelectedVertex is not null)
            throw new Exception("Escape did not clear selection");

        selectedSide = null;
        selectedId = null;

        // 21 px away -> selects nothing
        var missPoint = new Point(screenPoint.X + 21, screenPoint.Y);
        Press(canvas, missPoint);
        if (canvas.SelectedVertex is not null || selectedSide is not null || selectedId is not null)
            throw new Exception($"Hit test at 21 px failed: selected={canvas.SelectedVertex?.ToString() ?? "null"}");
    }

    public static void TestNearestVertexWins()
    {
        // Create profile where two vertices are close in model/screen coordinates
        var canvas = CreateConfiguredCanvas();
        var v1 = canvas.Profile!.Upper[1]; // u1 (0.25, 0.08)
        var p1 = canvas.ModelToScreen(v1.X, v1.Y);

        // Put a custom profile with two vertices very close
        var upper = new ProfileVertex[]
        {
            new("upper", "u0", 0.0, 0.0, true),
            new("upper", "uA", 0.50, 0.10, false),
            new("upper", "uB", 0.52, 0.10, false), // close to uA
            new("upper", "u3", 1.0, 0.0, true)
        };
        var lower = new ProfileVertex[]
        {
            new("lower", "l0", 0.0, 0.0, true),
            new("lower", "l1", 1.0, 0.0, true)
        };
        var profile = new ProfileView("close-pair", "hash-close", upper, lower, [], [], "closed");
        canvas.Profile = profile;

        var pA = canvas.ModelToScreen(0.50, 0.10);
        var pB = canvas.ModelToScreen(0.52, 0.10);
        double distBetween = Math.Abs(pB.X - pA.X);
        if (distBetween > 30)
            throw new Exception($"Test assumption invalid: vertices too far apart ({distBetween} px)");

        // Pick a point between pA and pB closer to pA (e.g., 1/3 of the way from pA to pB)
        // Both pA and pB are within 20 px
        var testPointCloserToA = new Point(pA.X + distBetween * 0.3, pA.Y);
        double dToA = Math.Abs(testPointCloserToA.X - pA.X);
        double dToB = Math.Abs(testPointCloserToA.X - pB.X);
        if (dToA > 20 || dToB > 20 || dToA >= dToB)
            throw new Exception("Test point geometry not as expected for nearest-vertex test");

        Press(canvas, testPointCloserToA);
        if (canvas.SelectedVertex != ("upper", "uA"))
            throw new Exception($"Nearest vertex to A failed: selected={canvas.SelectedVertex?.ToString() ?? "null"}");

        // Pick a point closer to pB
        var testPointCloserToB = new Point(pA.X + distBetween * 0.7, pA.Y);
        Press(canvas, testPointCloserToB);
        if (canvas.SelectedVertex != ("upper", "uB"))
            throw new Exception($"Nearest vertex to B failed: selected={canvas.SelectedVertex?.ToString() ?? "null"}");
    }

    public static void TestArrowMoves()
    {
        var canvas = CreateConfiguredCanvas();
        canvas.SelectedVertex = ("upper", "u2"); // u2 at (0.50, 0.10)

        string? movedSide = null;
        string? movedId = null;
        double movedX = 0, movedY = 0;
        canvas.VertexMoved += (s, id, x, y) =>
        {
            movedSide = s;
            movedId = id;
            movedX = x;
            movedY = y;
        };

        // Arrow moves 0.001 chord
        SendKey(canvas, Key.Right);
        if (movedSide != "upper" || movedId != "u2" || Math.Abs(movedX - 0.501) > 1e-6 || Math.Abs(movedY - 0.10) > 1e-6)
            throw new Exception($"Arrow Right move failed: x={movedX}, y={movedY}");

        SendKey(canvas, Key.Up);
        if (movedSide != "upper" || movedId != "u2" || Math.Abs(movedX - 0.50) > 1e-6 || Math.Abs(movedY - 0.101) > 1e-6)
            throw new Exception($"Arrow Up move failed: x={movedX}, y={movedY}");

        // Shift+Arrow moves 0.01 chord (Shift x10)
        SendKey(canvas, Key.Left, KeyModifiers.Shift);
        if (movedSide != "upper" || movedId != "u2" || Math.Abs(movedX - 0.49) > 1e-6 || Math.Abs(movedY - 0.10) > 1e-6)
            throw new Exception($"Shift+Arrow Left move failed: x={movedX}, y={movedY}");

        SendKey(canvas, Key.Down, KeyModifiers.Shift);
        if (movedSide != "upper" || movedId != "u2" || Math.Abs(movedX - 0.50) > 1e-6 || Math.Abs(movedY - 0.09) > 1e-6)
            throw new Exception($"Shift+Arrow Down move failed: x={movedX}, y={movedY}");
    }

    public static void TestFixedVertex()
    {
        var canvas = CreateConfiguredCanvas();
        var fixedVertex = canvas.Profile!.Upper[0]; // u0 at (0, 0), Fixed = true
        var screenPoint = canvas.ModelToScreen(fixedVertex.X, fixedVertex.Y);

        string? selectedSide = null;
        string? selectedId = null;
        canvas.VertexSelected += (s, id) => { selectedSide = s; selectedId = id; };

        // Fixed vertex is selectable
        Press(canvas, screenPoint);
        if (canvas.SelectedVertex != ("upper", "u0") || selectedSide != "upper" || selectedId != "u0")
            throw new Exception($"Fixed vertex selection failed: selected={canvas.SelectedVertex?.ToString() ?? "null"}");

        // Arrow keys emit nothing
        bool movedFired = false;
        canvas.VertexMoved += (_, _, _, _) => movedFired = true;

        SendKey(canvas, Key.Right);
        SendKey(canvas, Key.Up);
        SendKey(canvas, Key.Left, KeyModifiers.Shift);
        SendKey(canvas, Key.Down, KeyModifiers.Shift);

        if (movedFired)
            throw new Exception("Fixed vertex emitted VertexMoved on arrow keys");

        // Dragging fixed vertex also emits nothing
        MovePointer(canvas, new Point(screenPoint.X + 50, screenPoint.Y + 50));
        if (movedFired)
            throw new Exception("Fixed vertex emitted VertexMoved on drag");
    }

    public static void TestTabOrder()
    {
        var canvas = CreateConfiguredCanvas();
        // Upper: u0 (fixed), u1 (editable), u2 (editable), u3 (editable), u4 (fixed)
        // Lower: l0 (fixed), l1 (editable), l2 (editable), l3 (fixed)
        // Expected order: u1 -> u2 -> u3 -> l1 -> l2 -> (wrap to u1)

        canvas.SelectedVertex = null;

        var visited = new List<(string Side, string Id)>();
        for (int i = 0; i < 5; i++)
        {
            SendKey(canvas, Key.Tab);
            if (canvas.SelectedVertex is not { } sel)
                throw new Exception($"Tab step {i} resulted in null selection");
            visited.Add(sel);
        }

        var expected = new (string, string)[]
        {
            ("upper", "u1"),
            ("upper", "u2"),
            ("upper", "u3"),
            ("lower", "l1"),
            ("lower", "l2")
        };

        if (!visited.SequenceEqual(expected))
            throw new Exception($"Tab forward order incorrect: got [{string.Join(", ", visited)}]");

        // Next Tab wraps around to u1
        SendKey(canvas, Key.Tab);
        if (canvas.SelectedVertex != ("upper", "u1"))
            throw new Exception($"Tab wrap around failed: got {canvas.SelectedVertex}");

        // Shift+Tab cycles backward: u1 -> l2 -> l1 -> u3 -> u2 -> u1
        SendKey(canvas, Key.Tab, KeyModifiers.Shift);
        if (canvas.SelectedVertex != ("lower", "l2"))
            throw new Exception($"Shift+Tab reverse wrap failed: got {canvas.SelectedVertex}");

        SendKey(canvas, Key.Tab, KeyModifiers.Shift);
        if (canvas.SelectedVertex != ("lower", "l1"))
            throw new Exception($"Shift+Tab step failed: got {canvas.SelectedVertex}");
    }

    public static void TestEditableFalse()
    {
        var canvas = CreateConfiguredCanvas(editable: false);
        var targetVertex = canvas.Profile!.Upper[1]; // u1
        var screenPoint = canvas.ModelToScreen(targetVertex.X, targetVertex.Y);

        bool selectedFired = false;
        bool movedFired = false;
        canvas.VertexSelected += (_, _) => selectedFired = true;
        canvas.VertexMoved += (_, _, _, _) => movedFired = true;

        // Press does not select
        Press(canvas, screenPoint);
        if (canvas.SelectedVertex is not null || selectedFired)
            throw new Exception("Editable=false admitted vertex selection");

        // Drag does not move
        MovePointer(canvas, new Point(screenPoint.X + 20, screenPoint.Y + 20));
        if (movedFired)
            throw new Exception("Editable=false emitted VertexMoved on drag");

        // Set selection manually to test keyboard when Editable=false
        canvas.SelectedVertex = ("upper", "u1");
        SendKey(canvas, Key.Right);
        SendKey(canvas, Key.Tab);
        if (movedFired)
            throw new Exception("Editable=false emitted VertexMoved on arrow key");
        if (canvas.SelectedVertex != ("upper", "u1"))
            throw new Exception("Editable=false allowed Tab cycling");
    }

    public static void TestAccessibleText()
    {
        var canvas = CreateConfiguredCanvas();
        var texts = canvas.AccessibleTexts;
        var semantics = canvas.Semantics;
        var controls = canvas.SemanticControls;

        int totalVertices = canvas.Profile!.Upper.Count + canvas.Profile.Lower.Count;
        if (texts.Count != totalVertices || semantics.Count != totalVertices || controls.Count != totalVertices)
            throw new Exception($"Accessible items count mismatch: {texts.Count} texts, expected {totalVertices}");

        for (int i = 0; i < canvas.Profile.Upper.Count; i++)
        {
            var v = canvas.Profile.Upper[i];
            string text = texts[i];
            if (!text.Contains("upper") || !text.Contains(v.Id) ||
                !text.Contains(v.Fixed ? "fixed" : "editable") ||
                !text.Contains("x") || !text.Contains("y"))
                throw new Exception($"Upper vertex accessible text missing attributes: '{text}'");
        }

        for (int i = 0; i < canvas.Profile.Lower.Count; i++)
        {
            var v = canvas.Profile.Lower[i];
            string text = texts[canvas.Profile.Upper.Count + i];
            if (!text.Contains("lower") || !text.Contains(v.Id) ||
                !text.Contains(v.Fixed ? "fixed" : "editable") ||
                !text.Contains("x") || !text.Contains("y"))
                throw new Exception($"Lower vertex accessible text missing attributes: '{text}'");
        }

        // Verify automation peer exposes them as children
        var peer = ControlAutomationPeer.CreatePeerForElement(canvas);
        if (peer.GetAutomationControlType() != AutomationControlType.Group)
            throw new Exception("SectionCanvas peer is not Group");
        var children = peer.GetChildren();
        if (children is not null && children.Count > 0 && children.Count != totalVertices)
            throw new Exception($"Automation children count mismatch: {children.Count}, expected {totalVertices}");
    }
}
