namespace CalFocus.App.Views.Pages;

public sealed partial class SchedulePage : Page
{
    private readonly DispatcherTimer _clockTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    public SchedulePage()
    {
        InitializeComponent();

        _clockTimer.Tick += (_, _) => UpdateClock();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;

        UpdateClock();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _clockTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _clockTimer.Stop();
    }

    private void UpdateClock()
    {
        ClockText.Text = DateTime.Now.ToString("HH:mm:ss");
    }
}
