using Avalonia.Controls;

namespace CfdWorkbench.Desktop;

/// <summary>The document areas in flow order hosted inside the ModelArea workspace.</summary>
public enum WorkbenchArea
{
    Setup,
    Cad,
    Analysis,
    Experiment,
    Run,
    Results,
    Notebook
}

/// <summary>Live derived readiness badges rendered on the Left Activity Rail.</summary>
public sealed record ActivityRailBadges(
    string Setup,
    string Cad,
    string Analysis,
    string Experiment,
    string Run,
    string Results,
    string Export,
    string SolverSetup,
    string Settings = "No key",
    string Notebook = "Active",
    bool ShowNotebook = false);

/// <summary>
/// 7-Area Left Activity Rail (Ruling 277: single canonical area switcher). Pure Avalonia control with zero
/// shell-layout coupling; ShellHost subscribes to its navigation events and owns all tab and panel orchestration.
/// </summary>
public partial class ActivityRail : UserControl
{
    public event Action<WorkbenchArea>? AreaSelected;
    public event Action? ExportRequested;
    public event Action? SolverSetupRequested;
    public event Action? SettingsRequested;

    public WorkbenchArea ActiveArea { get; private set; } = WorkbenchArea.Cad;

    public bool IsSolverSetupActive { get; private set; }

    public Button ActiveButton => IsSolverSetupActive
        ? RailSolverSetupButton
        : ActiveArea switch
        {
            WorkbenchArea.Notebook => RailNotebookButton,
            WorkbenchArea.Setup => RailSetupButton,
            WorkbenchArea.Analysis => RailAnalysisButton,
            WorkbenchArea.Experiment => RailExperimentButton,
            WorkbenchArea.Run => RailRunButton,
            WorkbenchArea.Results => RailResultsButton,
            _ => RailCadButton
        };

    public ActivityRail()
    {
        InitializeComponent();
        RailNotebookButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Notebook);
        RailSetupButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Setup);
        RailCadButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Cad);
        RailAnalysisButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Analysis);
        RailExperimentButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Experiment);
        RailRunButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Run);
        RailResultsButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Results);
        RailExportButton.Click += (_, _) => ExportRequested?.Invoke();
        RailSolverSetupButton.Click += (_, _) => SolverSetupRequested?.Invoke();
        RailSettingsButton.Click += (_, _) => SettingsRequested?.Invoke();
    }

    public void BindState(WorkbenchArea activeArea, ActivityRailBadges badges, bool solverSetupActive = false)
    {
        ArgumentNullException.ThrowIfNull(badges);
        ActiveArea = activeArea;
        IsSolverSetupActive = solverSetupActive;

        BadgeNotebookText.Text = badges.Notebook;
        RailNotebookButton.IsVisible = badges.ShowNotebook || activeArea == WorkbenchArea.Notebook;
        BadgeSetupText.Text = badges.Setup;
        BadgeCadText.Text = badges.Cad;
        BadgeAnalysisText.Text = badges.Analysis;
        BadgeExperimentText.Text = badges.Experiment;
        BadgeRunText.Text = badges.Run;
        BadgeResultsText.Text = badges.Results;
        BadgeExportText.Text = badges.Export;
        BadgeSolverSetupText.Text = badges.SolverSetup;
        BadgeSettingsText.Text = badges.Settings;

        RailNotebookButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Notebook);
        RailSetupButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Setup);
        RailCadButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Cad);
        RailAnalysisButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Analysis);
        RailExperimentButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Experiment);
        RailRunButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Run);
        RailResultsButton.Classes.Set("active", !solverSetupActive && activeArea == WorkbenchArea.Results);
        RailSolverSetupButton.Classes.Set("active", solverSetupActive);
    }
}
