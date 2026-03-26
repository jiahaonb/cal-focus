namespace CalFocus.App.Views.Pages;

public sealed partial class WidgetCenterPage : Page
{
    private App CurrentApp => (App)Application.Current;

    public WidgetCenterPage()
    {
        InitializeComponent();

        CurrentApp.WidgetHostService.WidgetsChanged += OnWidgetsChanged;
        Unloaded += OnPageUnloaded;

        RefreshCount();
    }

    private void OnAddClockWidgetClick(object sender, RoutedEventArgs e)
    {
        _ = CurrentApp.WidgetHostService.CreateClockWidget();
    }

    private void OnAddScheduleWidgetClick(object sender, RoutedEventArgs e)
    {
        _ = CurrentApp.WidgetHostService.CreateScheduleWidget();
    }

    private void OnCopyLastWidgetClick(object sender, RoutedEventArgs e)
    {
        _ = CurrentApp.WidgetHostService.CopyLastWidget();
    }

    private void OnRemoveLastWidgetClick(object sender, RoutedEventArgs e)
    {
        _ = CurrentApp.WidgetHostService.RemoveLastWidget();
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
