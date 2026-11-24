using Avalonia.Controls;

namespace CfdWorkbench.Desktop;

/// <summary>The 6 document areas in flow order hosted inside the ModelArea workspace.</summary>
public enum WorkbenchArea
{
    Setup,
    Cad,
    Analysis,
    Experiment,
    Run,
    Results
}

/// <summary>Live derived readiness badges rendered on the 7-Area Left Activity Rail.</summary>
public sealed record ActivityRailBadges(
    string Setup,
    string Cad,
    string Analysis,
    string Experiment,
    string Run,
    string Results,
    string Export,
    string SolverSetup);

/// <summary>
/// 7-Area Left Activity Rail (B1, Rulings 271 &amp; 272, C4). Pure Avalonia control with zero shell-layout coupling;
/// ShellHost subscribes to its three navigation events and owns all tab and panel orchestration.
/// </summary>
public partial class ActivityRail : UserControl
{
    public event Action<WorkbenchArea>? AreaSelected;
    public event Action? ExportRequested;
    public event Action? SolverSetupRequested;

    public WorkbenchArea ActiveArea { get; private set; } = WorkbenchArea.Cad;

    public ActivityRail()
    {
        InitializeComponent();
        RailSetupButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Setup);
        RailCadButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Cad);
        RailAnalysisButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Analysis);
        RailExperimentButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Experiment);
        RailRunButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Run);
        RailResultsButton.Click += (_, _) => AreaSelected?.Invoke(WorkbenchArea.Results);
        RailExportButton.Click += (_, _) => ExportRequested?.Invoke();
        RailSolverSetupButton.Click += (_, _) => SolverSetupRequested?.Invoke();
    }

    public void BindState(WorkbenchArea activeArea, ActivityRailBadges badges)
    {
        ArgumentNullException.ThrowIfNull(badges);
        ActiveArea = activeArea;

        BadgeSetupText.Text = badges.Setup;
        BadgeCadText.Text = badges.Cad;
        BadgeAnalysisText.Text = badges.Analysis;
        BadgeExperimentText.Text = badges.Experiment;
        BadgeRunText.Text = badges.Run;
        BadgeResultsText.Text = badges.Results;
        BadgeExportText.Text = badges.Export;
        BadgeSolverSetupText.Text = badges.SolverSetup;

        RailSetupButton.Classes.Set("active", activeArea == WorkbenchArea.Setup);
        RailCadButton.Classes.Set("active", activeArea == WorkbenchArea.Cad);
        RailAnalysisButton.Classes.Set("active", activeArea == WorkbenchArea.Analysis);
        RailExperimentButton.Classes.Set("active", activeArea == WorkbenchArea.Experiment);
        RailRunButton.Classes.Set("active", activeArea == WorkbenchArea.Run);
        RailResultsButton.Classes.Set("active", activeArea == WorkbenchArea.Results);
    }
}
