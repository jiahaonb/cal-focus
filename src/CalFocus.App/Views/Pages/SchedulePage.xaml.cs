using CalFocus.App.Services;
using System.Globalization;

namespace CalFocus.App.Views.Pages;

public sealed partial class SchedulePage : Page
{
    private App CurrentApp => (App)Application.Current;
    private DispatcherTimer? _timeUpdateTimer;

    public SchedulePage()
    {
        InitializeComponent();

        CurrentApp.ScheduleBoardService.SchedulesChanged += OnSchedulesChanged;
        Unloaded += OnPageUnloaded;

        // 初始化时间更新
        InitializeTimeUpdate();
        
        // 初始化数据
        RefreshAllSchedules();
        RefreshTodos();
        RefreshReminders();
    }

    private void InitializeTimeUpdate()
    {
        _timeUpdateTimer = new DispatcherTimer();
        _timeUpdateTimer.Interval = TimeSpan.FromSeconds(1);
        _timeUpdateTimer.Tick += (_, _) => UpdateCurrentTime();
        _timeUpdateTimer.Start();
        
        UpdateCurrentTime();
    }

    private void UpdateCurrentTime()
    {
        var now = DateTime.Now;
        CurrentTimeText.Text = now.ToString("HH:mm");
        CurrentDateText.Text = now.ToString("yyyy年MM月dd日 dddd", new CultureInfo("zh-CN"));
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        CurrentApp.ScheduleBoardService.SchedulesChanged -= OnSchedulesChanged;
        Unloaded -= OnPageUnloaded;
        
        if (_timeUpdateTimer != null)
        {
            _timeUpdateTimer.Stop();
            _timeUpdateTimer = null;
        }
    }

    private void OnSchedulesChanged()
    {
        _ = DispatcherQueue.TryEnqueue(RefreshAllSchedules);
    }

    private void RefreshAllSchedules()
    {
        var entries = CurrentApp.ScheduleBoardService.GetAll();
        AllScheduleList.ItemsSource = entries;
        ScheduleSummaryText.Text = $"共 {entries.Count} 条";
    }

    private void RefreshTodos()
    {
        // TODO: 从 TodoService 获取待办列表
        var todos = new List<TodoEntry>();
        TodoListHome.ItemsSource = todos;
        TodoCountText.Text = $"共 {todos.Count} 项";
    }

    private void RefreshReminders()
    {
        // TODO: 从 ReminderService 获取提醒列表
        var reminders = new List<ReminderEntry>();
        ReminderListHome.ItemsSource = reminders;
        ReminderCountText.Text = $"共 {reminders.Count} 项";
    }
}

public sealed class TodoEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateOnly? DueDate { get; set; }
}

public sealed class ReminderEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset EventDate { get; set; }  // 事件日期
    public DateTimeOffset ReminderDate { get; set; }  // 提醒日期
    public string? RepeatRule { get; set; }

    public string DisplayEventDate => $"事件日期：{EventDate:yyyy-MM-dd}";
    public string DisplayReminderDate => $"提醒日期：{ReminderDate:yyyy-MM-dd HH:mm}";
}
