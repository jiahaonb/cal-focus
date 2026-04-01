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
        CurrentApp.ProductivityService.DataChanged += OnProductivityChanged;
        Unloaded += OnPageUnloaded;

        InitializeTimeUpdate();
        RefreshAllSchedules();
        RefreshTodos();
        RefreshReminders();

        WeatherTempText.Text = "--°C";
        WeatherStatusText.Text = "等待天气 API 接入";
    }

    private void InitializeTimeUpdate()
    {
        _timeUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timeUpdateTimer.Tick += (_, _) => UpdateCurrentTime();
        _timeUpdateTimer.Start();

        UpdateCurrentTime();
    }

    private void UpdateCurrentTime()
    {
        var now = DateTime.Now;
        CurrentTimeText.Text = now.ToString("HH:mm:ss");
        CurrentDateText.Text = now.ToString("yyyy年MM月dd日 dddd", new CultureInfo("zh-CN"));
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        CurrentApp.ScheduleBoardService.SchedulesChanged -= OnSchedulesChanged;
        CurrentApp.ProductivityService.DataChanged -= OnProductivityChanged;
        Unloaded -= OnPageUnloaded;

        if (_timeUpdateTimer is not null)
        {
            _timeUpdateTimer.Stop();
            _timeUpdateTimer = null;
        }
    }

    private void OnSchedulesChanged()
    {
        _ = DispatcherQueue.TryEnqueue(RefreshAllSchedules);
    }

    private void OnProductivityChanged()
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            RefreshTodos();
            RefreshReminders();
        });
    }

    private void RefreshAllSchedules()
    {
        var entries = CurrentApp.ScheduleBoardService.GetAll();
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);

        var upcoming = entries
            .Where(x => x.Date > today || (x.Date == today && (x.StartTime ?? TimeSpan.MaxValue) >= now.TimeOfDay))
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime ?? TimeSpan.MaxValue)
            .Take(60)
            .ToList();

        if (upcoming.Count == 0)
        {
            upcoming = entries
                .OrderBy(x => x.Date)
                .ThenBy(x => x.StartTime ?? TimeSpan.MaxValue)
                .Take(60)
                .ToList();
        }

        var viewItems = upcoming
            .Select(x => new HomeStripItem
            {
                Title = x.Title,
                Subtitle = FormatScheduleSubtitle(x.Date, x.StartTime)
            })
            .ToList();

        AllScheduleList.ItemsSource = viewItems;
        ScheduleSummaryText.Text = $"共 {viewItems.Count} 条";
    }

    private void RefreshTodos()
    {
        var now = DateTimeOffset.Now;
        var todos = CurrentApp.ProductivityService
            .GetTodos()
            .Where(x => x.DueAt >= now.AddDays(-1))
            .Take(60)
            .Select(x => new HomeStripItem
            {
                Title = x.Title,
                Subtitle = $"DDL {x.DueAt:MM-dd HH:mm}"
            })
            .ToList();

        TodoListHome.ItemsSource = todos;
        TodoCountText.Text = $"共 {todos.Count} 项";
    }

    private void RefreshReminders()
    {
        var now = DateTimeOffset.Now;
        var reminders = CurrentApp.ProductivityService
            .GetReminders()
            .Where(x => x.ReminderAt >= now.AddDays(-1))
            .Take(60)
            .Select(x => new HomeStripItem
            {
                Title = x.Title,
                Subtitle = $"{x.ReminderAt:MM-dd HH:mm} 提醒 · {x.EventAt:MM-dd HH:mm} 事件"
            })
            .ToList();

        ReminderListHome.ItemsSource = reminders;
        ReminderCountText.Text = $"共 {reminders.Count} 项";
    }

    private async void OnAddScheduleClick(object sender, RoutedEventArgs e)
    {
        var titleBox = new TextBox
        {
            Header = "日程标题",
            PlaceholderText = "输入事件名称"
        };

        var datePicker = new DatePicker
        {
            Header = "日期",
            Date = DateTimeOffset.Now.Date
        };

        var timePicker = new TimePicker
        {
            Header = "开始时间",
            Time = DateTime.Now.TimeOfDay
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children = { titleBox, datePicker, timePicker }
        };

        var dialog = new ContentDialog
        {
            Title = "新增日程",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(titleBox.Text))
        {
            return;
        }

        var date = DateOnly.FromDateTime(datePicker.Date.DateTime);
        CurrentApp.ScheduleBoardService.Add(titleBox.Text.Trim(), date, timePicker.Time, "无");
    }

    private async void OnAddTodoClick(object sender, RoutedEventArgs e)
    {
        var titleBox = new TextBox
        {
            Header = "待办名称",
            PlaceholderText = "输入待办标题"
        };
        var dueDatePicker = new DatePicker
        {
            Header = "DDL 日期",
            Date = DateTimeOffset.Now.Date.AddDays(1)
        };
        var dueTimePicker = new TimePicker
        {
            Header = "DDL 时间",
            Time = new TimeSpan(18, 0, 0)
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children = { titleBox, dueDatePicker, dueTimePicker }
        };

        var dialog = new ContentDialog
        {
            Title = "新增待办",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(titleBox.Text))
        {
            return;
        }

        var dueAt = dueDatePicker.Date.Date + dueTimePicker.Time;
        CurrentApp.ProductivityService.AddTodo(titleBox.Text, dueAt);
    }

    private async void OnAddReminderClick(object sender, RoutedEventArgs e)
    {
        var titleBox = new TextBox
        {
            Header = "提醒标题",
            PlaceholderText = "例如：项目站会"
        };

        var eventDatePicker = new DatePicker
        {
            Header = "事件日期",
            Date = DateTimeOffset.Now.Date
        };
        var eventTimePicker = new TimePicker
        {
            Header = "事件时间",
            Time = DateTime.Now.TimeOfDay
        };

        var remindDatePicker = new DatePicker
        {
            Header = "提醒日期",
            Date = DateTimeOffset.Now.Date
        };
        var remindTimePicker = new TimePicker
        {
            Header = "提醒时间",
            Time = DateTime.Now.AddMinutes(10).TimeOfDay
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                titleBox,
                eventDatePicker,
                eventTimePicker,
                remindDatePicker,
                remindTimePicker
            }
        };

        var dialog = new ContentDialog
        {
            Title = "新增提醒",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(titleBox.Text))
        {
            return;
        }

        var eventAt = eventDatePicker.Date.Date + eventTimePicker.Time;
        var remindAt = remindDatePicker.Date.Date + remindTimePicker.Time;

        CurrentApp.ProductivityService.AddReminder(titleBox.Text, eventAt, remindAt);
    }

    private static string FormatScheduleSubtitle(DateOnly date, TimeSpan? startTime)
    {
        var timeText = startTime.HasValue ? startTime.Value.ToString(@"hh\:mm") : "全天";
        return $"{date:MM-dd} {timeText}";
    }
}

public sealed class HomeStripItem
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
}
