using CalFocus.App.Services;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Globalization;

namespace CalFocus.App.Views.Pages;

public sealed partial class CalendarSchedulePage : Page
{
    private static readonly string[] RepeatRules = ["无", "每天", "每周", "每月"];
    private static readonly Dictionary<string, string> ColorMap = new()
    {
        { "#FF6B6B", "红色" },
        { "#FFA500", "橙色" },
        { "#FFD700", "黄色" },
        { "#4CAF50", "绿色" },
        { "#2196F3", "蓝色" },
        { "#9C27B0", "紫色" }
    };

    private App CurrentApp => (App)Application.Current;
    private DateTime _currentMonth = DateTime.Now;
    private Guid? _editingScheduleId;
    private DateOnly? _editingDate;
    private string _selectedColor = "#2196F3";
    private DateTime _lastTapTime = DateTime.MinValue;
    private Guid? _lastTappedScheduleId;

    public CalendarSchedulePage()
    {
        InitializeComponent();

        CurrentApp.ScheduleBoardService.SchedulesChanged += OnSchedulesChanged;
        Unloaded += OnPageUnloaded;

        RefreshCalendar();
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        CurrentApp.ScheduleBoardService.SchedulesChanged -= OnSchedulesChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void OnSchedulesChanged()
    {
        _ = DispatcherQueue.TryEnqueue(RefreshCalendar);
    }

    private void RefreshCalendar()
    {
        MonthYearText.Text = _currentMonth.ToString("yyyy年M月", new CultureInfo("zh-CN"));

        var items = new List<CalendarDayItem>();
        var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.Now);

        // 计算月份第一天是星期几（0=周日，1=周一...6=周六）
        int startOffset = (int)firstDay.DayOfWeek;

        // 前置空白占位格
        for (int i = 0; i < startOffset; i++)
        {
            items.Add(new CalendarDayItem { IsPlaceholder = true });
        }

        // 获取该月所有日程
        var allSchedules = CurrentApp.ScheduleBoardService.GetAll();

        // 填充日期
        for (int i = 1; i <= lastDay.Day; i++)
        {
            var date = new DateOnly(_currentMonth.Year, _currentMonth.Month, i);
            var isToday = date == today;
            var daySchedules = allSchedules
                .Where(x => x.Date == date)
                .OrderBy(x => x.StartTime ?? TimeSpan.MaxValue)
                .ThenBy(x => x.Title)
                .Take(3)
                .Select(x => new ScheduleItemDisplay
                {
                    Id = x.Id,
                    Title = x.Title,
                    DisplayTime = x.StartTime.HasValue ? x.StartTime.Value.ToString(@"hh\:mm") : "全天",
                    ColorBrush = GetColorBrush(x.Id)
                })
                .ToList();

            items.Add(new CalendarDayItem
            {
                DayNumber = i.ToString(),
                Date = date,
                IsToday = isToday,
                DayForeground = isToday
                    ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 91, 127, 255))
                    : new SolidColorBrush(Windows.UI.Color.FromArgb(204, 255, 255, 255)),
                Schedules = daySchedules
            });
        }

        CalendarGrid.ItemsSource = items;
    }

    private SolidColorBrush GetColorBrush(Guid scheduleId)
    {
        var colors = new[] {
            Windows.UI.Color.FromArgb(255, 255, 107, 107),  // #FF6B6B
            Windows.UI.Color.FromArgb(255, 255, 149,   0),  // #FF9500
            Windows.UI.Color.FromArgb(255,  52, 199,  89),  // #34C759
            Windows.UI.Color.FromArgb(255,  91, 127, 255),  // #5B7FFF
            Windows.UI.Color.FromArgb(255, 175,  82, 222),  // #AF52DE
            Windows.UI.Color.FromArgb(255, 255,  55,  95),  // #FF375F
        };
        var c = colors[Math.Abs(scheduleId.GetHashCode()) % colors.Length];
        return new SolidColorBrush(c);
    }

    private void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        RefreshCalendar();
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        RefreshCalendar();
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = DateTime.Now;
        RefreshCalendar();
    }

    private void OnAddScheduleClick(object sender, RoutedEventArgs e)
    {
        _editingScheduleId = null;
        _editingDate = DateOnly.FromDateTime(DateTime.Now);
        _selectedColor = "#5B7FFF";
        EditTitleBox.Text = "";
        EditTimePicker.Time = new TimeSpan(9, 0, 0);
        OpenEditPopup(true, _editingDate.Value);
        EditPopup.IsOpen = true;
    }

    private void OnDayNumberClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not DateOnly date)
        {
            return;
        }

        var now = DateTime.Now;
        var timeSinceLastTap = (now - _lastTapTime).TotalMilliseconds;

        // 检测双击（300ms 内的两次点击）
        if (timeSinceLastTap < 300 && _lastTappedScheduleId == null)
        {
            // 双击日期数字 - 打开当天详情
            OpenDayDetails(date);
            _lastTapTime = DateTime.MinValue;
        }
        else
        {
            _lastTapTime = now;
            _lastTappedScheduleId = null;
        }
    }

    private void OpenDayDetails(DateOnly date)
    {
        // TODO: 打开当天详情页面
        // 这里可以导航到详情页或显示详情弹窗
    }

    private void OnSchedulesAreaDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not DateOnly date)
        {
            return;
        }

        _editingScheduleId = null;
        _editingDate = date;
        _selectedColor = "#5B7FFF";
        EditTitleBox.Text = "";
        EditTimePicker.Time = new TimeSpan(9, 0, 0);
        OpenEditPopup(true, date);
        EditPopup.IsOpen = true;
    }

    private void OnScheduleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        var now = DateTime.Now;
        var timeSinceLastTap = (now - _lastTapTime).TotalMilliseconds;

        // 检测双击（300ms 内的两次点击）
        if (_lastTappedScheduleId == id && timeSinceLastTap < 300)
        {
            // 双击日程 - 编辑
            EditSchedule(id);
            _lastTapTime = DateTime.MinValue;
            _lastTappedScheduleId = null;
        }
        else
        {
            // 记录本次点击
            _lastTapTime = now;
            _lastTappedScheduleId = id;
        }
    }

    private void EditSchedule(Guid id)
    {
        var schedules = CurrentApp.ScheduleBoardService.GetAll();
        var schedule = schedules.FirstOrDefault(x => x.Id == id);
        if (schedule is null) return;

        _editingScheduleId = id;
        _editingDate = schedule.Date;
        _selectedColor = "#5B7FFF";
        EditTitleBox.Text = schedule.Title;
        EditTimePicker.Time = schedule.StartTime ?? new TimeSpan(9, 0, 0);
        OpenEditPopup(false, schedule.Date);
        EditPopup.IsOpen = true;
    }

    private void OnColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string color)
        {
            _selectedColor = color;
        }
    }

    private void OnSaveScheduleClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(EditTitleBox.Text) || _editingDate is null)
        {
            return;
        }

        if (_editingScheduleId.HasValue)
        {
            // 编辑现有日程
            CurrentApp.ScheduleBoardService.Update(
                _editingScheduleId.Value,
                EditTitleBox.Text.Trim(),
                EditTimePicker.Time,
                "无"
            );
        }
        else
        {
            // 新增日程
            CurrentApp.ScheduleBoardService.Add(
                EditTitleBox.Text.Trim(),
                _editingDate.Value,
                EditTimePicker.Time,
                "无"
            );
        }

        EditPopup.IsOpen = false;
    }

    private void OnCancelEditClick(object sender, RoutedEventArgs e)
    {
        EditPopup.IsOpen = false;
    }

    private void OnDeleteScheduleClick(object sender, RoutedEventArgs e)
    {
        if (!_editingScheduleId.HasValue) return;
        CurrentApp.ScheduleBoardService.Delete(_editingScheduleId.Value);
        EditPopup.IsOpen = false;
    }

    private void OpenEditPopup(bool isNew, DateOnly date)
    {
        PopupTitleText.Text = isNew ? "新建日程" : "编辑日程";
        PopupDateText.Text = date.ToString("M月d日");
        DeleteBtn.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
    }
}

public sealed class CalendarDayItem
{
    public string DayNumber { get; set; } = "";
    public DateOnly Date { get; set; }
    public bool IsPlaceholder { get; set; } = false;
    public bool IsToday { get; set; } = false;
    public Brush DayForeground { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(204, 255, 255, 255));
    public List<ScheduleItemDisplay> Schedules { get; set; } = new();
}

public sealed class ScheduleItemDisplay
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string DisplayTime { get; set; } = "";
    public Brush ColorBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 91, 127, 255));
}
