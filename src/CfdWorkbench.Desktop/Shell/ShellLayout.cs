using Dock.Model.Core;
using Dock.Model.Controls;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace CfdWorkbench.Desktop.Shell;

public sealed class ShellLayoutFactory : Factory
{
    public ITool PropertiesTool { get; private set; } = null!;
    public ITool BrowserTool { get; private set; } = null!;
    public ITool RailControlsTool { get; private set; } = null!;
    public IDocument ModelDocument { get; private set; } = null!;
    public IDocument SamplesDocument { get; private set; } = null!;
    public IDocument SectionSampleDocument { get; private set; } = null!;
    public IDocument FoilSourceDocument { get; private set; } = null!;
    public IDocument SectionDocument { get; private set; } = null!;
    public IToolDock LeftToolDock { get; private set; } = null!;
    public IDocumentDock MainDocumentDock { get; private set; } = null!;
    public IProportionalDock TopProportionalDock { get; private set; } = null!;
    public IRootDock RootLayout { get; private set; } = null!;

    public override IRootDock CreateLayout()
    {
        PropertiesTool = new Tool
        {
            Id = "properties",
            Title = "Properties",
            CanPin = false,
            CanClose = true,
            CanFloat = true
        };

        BrowserTool = new Tool
        {
            Id = "browser",
            Title = "Browser",
            CanPin = false,
            CanClose = true,
            CanFloat = true
        };

        RailControlsTool = new Tool
        {
            Id = "rail-controls",
            Title = "Rail controls",
            CanPin = false,
            CanClose = true,
            CanFloat = true
        };

        ModelDocument = new Document
        {
            Id = "model",
            Title = "Plan",
            CanClose = false,
            CanFloat = false
        };

        SamplesDocument = new Document
        {
            Id = "3d-samples",
            Title = "3D samples",
            CanClose = false,
            CanFloat = false
        };

        SectionSampleDocument = new Document
        {
            Id = "section-sample",
            Title = "Section sample",
            CanClose = false,
            CanFloat = false
        };

        FoilSourceDocument = new Document
        {
            Id = "foil-source",
            Title = "Foil source",
            CanClose = false,
            CanFloat = false
        };

        SectionDocument = new Document
        {
            Id = "section",
            Title = "Section",
            CanClose = false,
            CanFloat = false
        };

        LeftToolDock = new ToolDock
        {
            Id = "LeftDock",
            Title = "Left side bar",
            Proportion = 0.25,
            Alignment = Alignment.Left,
            ActiveDockable = PropertiesTool,
            VisibleDockables = CreateList<IDockable>(PropertiesTool, BrowserTool, RailControlsTool)
        };

        MainDocumentDock = new DocumentDock
        {
            Id = "Docs",
            Title = "Model area",
            ActiveDockable = ModelDocument,
            VisibleDockables = CreateList<IDockable>(ModelDocument, SamplesDocument, SectionSampleDocument, FoilSourceDocument, SectionDocument),
            CanCreateDocument = false
        };

        TopProportionalDock = new ProportionalDock
        {
            Id = "Top",
            Orientation = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(LeftToolDock, new ProportionalDockSplitter(), MainDocumentDock)
        };

        var root = CreateRootDock();
        root.Id = "Root";
        root.ActiveDockable = TopProportionalDock;
        root.DefaultDockable = TopProportionalDock;
        root.VisibleDockables = CreateList<IDockable>(TopProportionalDock);

        RootLayout = root;
        InitLayout(root);
        return root;
    }

    public IDockable? FindDockable(string id) => id switch
    {
        "properties" => PropertiesTool,
        "browser" => BrowserTool,
        "rail-controls" => RailControlsTool,
        "model" => ModelDocument,
        "3d-samples" => SamplesDocument,
        "section-sample" => SectionSampleDocument,
        "foil-source" => FoilSourceDocument,
        "section" => SectionDocument,
        _ => FindDockable(RootLayout, id)
    };

    public static IDockable? FindDockable(IDockable? root, string id)
    {
        if (root == null) return null;
        if (root.Id == id) return root;
        if (root is IDock dock && dock.VisibleDockables != null)
        {
            foreach (var child in dock.VisibleDockables)
            {
                var found = FindDockable(child, id);
                if (found != null) return found;
            }
        }
        return null;
    }

    public static IDock? FindParentDock(IDockable? root, IDockable target)
    {
        if (root is IDock dock && dock.VisibleDockables != null)
        {
            if (dock.VisibleDockables.Contains(target)) return dock;
            foreach (var child in dock.VisibleDockables)
            {
                var parent = FindParentDock(child, target);
                if (parent != null) return parent;
            }
        }
        return null;
    }
}
