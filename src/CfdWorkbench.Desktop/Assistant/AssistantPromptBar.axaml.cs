using Avalonia.Automation;
using Avalonia.Controls;
using CfdWorkbench.Core.Assistant;

namespace CfdWorkbench.Desktop.Assistant;

/// <summary>
/// Reusable assistant entry bar for CAD (AI-07), Analysis (AI-08), and Experiment (AI-09) surfaces
/// (docs/design/byok-assistants.md §4.1, Wave 12B R2-08). Supports compact stacked headers for narrow columns,
/// metered daily cap display ("Used X/Y today"), and attaches every visible button's Click handler in the
/// constructor (UI_DEAD_CONTROL_ShellButtonsHaveActions).
/// </summary>
public partial class AssistantPromptBar : UserControl
{
    private bool compactHeader;

    public event Action? ConfigureKeyRequested;
    public event Action<string>? PromptSubmitted;

    public AssistantPromptBar()
    {
        InitializeComponent();
        ConfigureKeyButton.Content = AssistantCopy.ConfigureKeyButton;
        OpenSettingsButton.Content = AssistantCopy.OpenSettingsButton;

        ConfigureKeyButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        OpenSettingsButton.Click += (_, _) => ConfigureKeyRequested?.Invoke();
        SubmitButton.Click += (_, _) =>
        {
            string prompt = (PromptBox.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(prompt))
                PromptSubmitted?.Invoke(prompt);
        };
    }

    /// <summary>When true, stacks status/cap/action chips on a second row beneath the entry title for narrow columns.</summary>
    public bool CompactHeader
    {
        get => compactHeader;
        set
        {
            if (compactHeader == value) return;
            compactHeader = value;
            ApplyHeaderLayout();
        }
    }

    public void Configure(string entryTitle, string submitLabel, string watermark, string? submitAutomationName = null)
    {
        EntryTitleText.Text = entryTitle;
        SubmitButton.Content = submitLabel;
        PromptBox.Watermark = watermark;
        AutomationProperties.SetName(PromptBox, entryTitle);
        AutomationProperties.SetName(SubmitButton, submitAutomationName ?? submitLabel);
    }

    public void FocusPrompt()
    {
        if (PromptBox.IsEnabled)
        {
            PromptBox.Focus();
            PromptBox.SelectAll();
        }
    }

    public void RefreshStatus(AssistantRuntimeHub? hub)
    {
        bool hasKey = hub?.HasApiKey == true;
        bool evaluated = hub?.Eligibility.ProposalsEnabled == true;
        bool ready = hasKey && evaluated;

        ConfigureKeyButton.IsVisible = !hasKey;
        OpenSettingsButton.IsVisible = hasKey && !evaluated;
        PromptBox.IsEnabled = ready;
        SubmitButton.IsEnabled = ready;

        if (hub is not null)
        {
            UsageChipText.Text = AssistantCopy.DailyUsageChip(hub.DailyRequestsUsedUtc, hub.CurrentSettings.DailyRequestCap);
            UsageChipBorder.IsVisible = true;
        }
        else
        {
            UsageChipBorder.IsVisible = false;
        }

        if (!hasKey)
        {
            StatusBadgeText.Text = AssistantCopy.BadgeNoKey;
            StatusDetailText.Text = AssistantCopy.NoKeyNotice;
            StatusDetailText.IsVisible = true;
        }
        else if (!evaluated)
        {
            StatusBadgeText.Text = AssistantCopy.UnevaluatedOnThisModel;
            StatusDetailText.Text = AssistantCopy.UnevaluatedDetail(AssistantEvalSuite.EvalThresholdsVersion);
            StatusDetailText.IsVisible = true;
        }
        else
        {
            StatusBadgeText.Text = $"Ready · {hub!.CurrentSettings.ModelId}";
            StatusDetailText.IsVisible = false;
        }
    }

    public void ShowFeedback(string message)
    {
        StatusDetailText.Text = message;
        StatusDetailText.IsVisible = !string.IsNullOrWhiteSpace(message);
    }

    private void ApplyHeaderLayout()
    {
        if (compactHeader)
        {
            Grid.SetColumnSpan(EntryTitleText, 2);
            Grid.SetRow(HeaderChipsPanel, 1);
            Grid.SetColumn(HeaderChipsPanel, 0);
            Grid.SetColumnSpan(HeaderChipsPanel, 2);
        }
        else
        {
            Grid.SetColumnSpan(EntryTitleText, 1);
            Grid.SetRow(HeaderChipsPanel, 0);
            Grid.SetColumn(HeaderChipsPanel, 1);
            Grid.SetColumnSpan(HeaderChipsPanel, 1);
        }
    }
}
