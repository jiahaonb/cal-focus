namespace CalFocus.App.Views.Pages;

public sealed partial class WidgetCenterPage : Page
{
    private App CurrentApp => (App)Application.Current;

    public WidgetCenterPage()
    {
        InitializeComponent();

        CurrentApp.WidgetHostService.WidgetsChanged += OnWidgetsChanged;
        Unloaded += OnPageUnloaded;

        WidgetPreviewGrid.ItemsSource = BuildPreviewCards();

        RefreshCount();
    }

    private List<WidgetPreviewCard> BuildPreviewCards()
    {
        return new List<WidgetPreviewCard>
        {
            new()
            {
                WidgetType = "Clock",
                Name = "桌面时钟",
                Description = "秒级刷新，适合放置在桌面角落进行实时时间查看。",
                PreviewLabel = "时间预览",
                PreviewPrimary = "14:30:45",
                PreviewSecondary = "2026/04/01"
            },
            new()
            {
                WidgetType = "Schedule",
                Name = "日程卡片",
                Description = "展示今天的关键安排，适合用于快速查看计划节奏。",
                PreviewLabel = "日程预览",
                PreviewPrimary = "09:30 站会",
                PreviewSecondary = "14:00 设计评审\n17:30 每日复盘"
            },
            new()
            {
                WidgetType = "Pomodoro",
                Name = "番茄钟",
                Description = "默认 15 分钟，可通过输入分钟数进行自定义。",
                PreviewLabel = "专注预览",
                PreviewPrimary = "15:00",
                PreviewSecondary = "结束后触发通知"
            }
        };
    }

    private void OnAddWidgetClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string widgetType)
        {
            return;
        }

        var themeTint = CurrentApp.UiSettingsService.Current.BrandColorHex;

        switch (widgetType)
        {
            case "Clock":
                _ = CurrentApp.WidgetHostService.CreateClockWidget(themeTint);
                break;
            case "Schedule":
                _ = CurrentApp.WidgetHostService.CreateScheduleWidget(themeTint);
                break;
            case "Pomodoro":
                _ = CurrentApp.WidgetHostService.CreatePomodoroWidget(themeTint);
                break;
        }
    }

    private void OnWidgetsChanged()
    {
        _ = DispatcherQueue.TryEnqueue(RefreshCount);
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        CurrentApp.WidgetHostService.WidgetsChanged -= OnWidgetsChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void RefreshCount()
    {
        var count = CurrentApp.WidgetHostService.ActiveWidgets.Count;
        WidgetCountText.Text = $"当前数量：{count}";
    }
}

public sealed class WidgetPreviewCard
{
    public string WidgetType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PreviewLabel { get; set; } = string.Empty;
    public string PreviewPrimary { get; set; } = string.Empty;
    public string PreviewSecondary { get; set; } = string.Empty;
}
