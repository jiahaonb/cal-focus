using CalFocus.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CalFocus.App.Views.Pages;

public sealed partial class CalendarSchedulePage : Page
{
    private static readonly Dictionary<string, string> ColorMap = new()
    {
        { "#0D5D56", "深绿" },
        { "#22C55E", "翠绿" },
        { "#F59E0B", "橙黄" },
        { "#EF4444", "赤红" },
        { "#3B82F6", "蔚蓝" },
        { "#8B5CF6", "靛紫" }
    };

    private App CurrentApp => (App)Application.Current;
    private DateTime _currentMonth = DateTime.Now;
    private Guid? _editingScheduleId;
    private DateOnly? _editingDate;
    private string _selectedColor = "#0D5D56";
    private DateTime _lastTapTime = DateTime.MinValue;
    
    private bool _isInitializing = true;

    public CalendarSchedulePage()
    {
        InitializeComponent();

        InitializeComboBoxes();

        CurrentApp.ScheduleBoardService.SchedulesChanged += OnSchedulesChanged;
        Unloaded += OnPageUnloaded;

        _isInitializing = false;
        RefreshCalendar();
    }

    private void InitializeComboBoxes()
    {
        for (int i = 2020; i <= 2050; i++)
        {
            YearCombo.Items.Add(i);
        }
        for (int i = 1; i <= 12; i++)
        {
            MonthCombo.Items.Add(i);
        }
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

    private void OnYearMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing) return;

        if (YearCombo.SelectedItem is int year && MonthCombo.SelectedItem is int month)
        {
            _currentMonth = new DateTime(year, month, 1);
            RefreshCalendar();
        }
    }

    private void RefreshCalendar()
    {
        _isInitializing = true;
        YearCombo.SelectedItem = _currentMonth.Year;
        MonthCombo.SelectedItem = _currentMonth.Month;
        _isInitializing = false;

        var firstDay = new DateTime(_currentMonth.Year, _currentMonth.Month, 1);
        var lastDay = firstDay.AddMonths(1).AddDays(-1);
        var today = DateOnly.FromDateTime(DateTime.Now);

        int startOffset = (int)firstDay.DayOfWeek;
        var allSchedules = CurrentApp.ScheduleBoardService.GetAll();

        DaysGrid.Children.Clear();
        var template = (DataTemplate)Resources["DayCellTemplate"];

        int currentDay = 1;
        bool started = false;

        for (int row = 0; row < 6; row++)
        {
            for (int col = 0; col < 7; col++)
            {
                if (!started && col >= startOffset)
                {
                    started = true;
                }

                CalendarDayItem item;

                if (started && currentDay <= lastDay.Day)
                {
                    var date = new DateOnly(_currentMonth.Year, _currentMonth.Month, currentDay);
                    var isToday = date == today;
                    var daySchedules = allSchedules
                        .Where(x => x.Date == date)
                        .OrderBy(x => x.StartTime ?? TimeSpan.MaxValue)
                        .ThenBy(x => x.Title)
                        .Select(x => new ScheduleItemDisplay
                        {
                            Id = x.Id,
                            Title = x.Title,
                            DisplayTime = x.StartTime.HasValue ? x.StartTime.Value.ToString(@"hh\:mm") : "全天",
                            ColorBrush = GetColorBrush(x.Id)
                        })
                        .ToList();

                    item = new CalendarDayItem
                    {
                        DayNumber = currentDay.ToString(),
                        Date = date,
                        IsToday = isToday,
                        DayForeground = isToday
                            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 93, 86)) // #0D5D56
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 28, 58, 51)), // #1C3A33
                        BorderBrush = isToday 
                            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 93, 86)) 
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 225, 235, 231)), // #E1EBE7
                        BackgroundBrush = isToday
                            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255))
                            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
                        Schedules = daySchedules.Take(4).ToList() // Only show up to 4 on calendar grid
                    };
                    currentDay++;
                }
                else
                {
                    item = new CalendarDayItem 
                    { 
                        IsPlaceholder = true,
                        DayForeground = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                        BackgroundBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                        BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0))
                    };
                }

                if (template.LoadContent() is FrameworkElement cell)
                {
                    cell.DataContext = item;
                    Grid.SetRow(cell, row);
                    Grid.SetColumn(cell, col);
                    DaysGrid.Children.Add(cell);
                }
            }
        }
    }

    private SolidColorBrush GetColorBrush(Guid scheduleId)
    {
        var colors = new[] {
            Windows.UI.Color.FromArgb(255, 13, 93, 86),  // #0D5D56
            Windows.UI.Color.FromArgb(255, 34, 197, 94),  // #22C55E
            Windows.UI.Color.FromArgb(255, 245, 158, 11),  // #F59E0B
            Windows.UI.Color.FromArgb(255, 239, 68, 68),  // #EF4444
            Windows.UI.Color.FromArgb(255, 59, 130, 246),  // #3B82F6
            Windows.UI.Color.FromArgb(255, 139, 92, 246),  // #8B5CF6
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
        OpenEditOverlay(null, DateOnly.FromDateTime(DateTime.Now));
    }

    private void OnDayNumberClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not DateOnly date)
        {
            return;
        }

        var now = DateTime.Now;
        var timeSinceLastTap = (now - _lastTapTime).TotalMilliseconds;

        OpenDayDetails(date);
    }

    private void OpenDayDetails(DateOnly date)
    {
        DetailDateTitle.Text = date.ToString("yyyy年M月d日");
        
        var allSchedules = CurrentApp.ScheduleBoardService.GetAll();
        var daySchedules = allSchedules
            .Where(x => x.Date == date)
            .OrderBy(x => x.StartTime ?? TimeSpan.MaxValue)
            .ThenBy(x => x.Title)
            .Select(x => new ScheduleItemDisplay
            {
                Id = x.Id,
                Title = x.Title,
                DisplayTime = x.StartTime.HasValue ? x.StartTime.Value.ToString(@"hh\:mm") : "全天",
                ColorBrush = GetColorBrush(x.Id)
            })
            .ToList();

        DetailSchedulesList.ItemsSource = daySchedules;
        DetailsOverlay.Visibility = Visibility.Visible;
    }

    private void OnCloseDetailsClick(object sender, RoutedEventArgs e)
    {
        DetailsOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnSchedulesAreaDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not DateOnly date)
        {
            return;
        }
        OpenEditOverlay(null, date);
    }

    private void OnScheduleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }
        EditSchedule(id);
    }

    private void EditSchedule(Guid id)
    {
        var schedules = CurrentApp.ScheduleBoardService.GetAll();
        var schedule = schedules.FirstOrDefault(x => x.Id == id);
        if (schedule is null) return;

        OpenEditOverlay(id, schedule.Date);
        EditTitleBox.Text = schedule.Title;
        EditTimePicker.Time = schedule.StartTime ?? new TimeSpan(9, 0, 0);
    }

    private void OpenEditOverlay(Guid? id, DateOnly date)
    {
        DetailsOverlay.Visibility = Visibility.Collapsed; // Close detail if open
        
        bool isNew = !id.HasValue;
        _editingScheduleId = id;
        _editingDate = date;
        _selectedColor = "#0D5D56";
        
        if (isNew)
        {
            EditTitleBox.Text = "";
            EditTimePicker.Time = new TimeSpan(9, 0, 0);
        }

        PopupTitleText.Text = isNew ? "新建日程" : "编辑日程";
        PopupDateText.Text = date.ToString("M月d日");
        DeleteBtn.Visibility = isNew ? Visibility.Collapsed : Visibility.Visible;
        
        EditOverlay.Visibility = Visibility.Visible;
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
            CurrentApp.ScheduleBoardService.Update(
                _editingScheduleId.Value,
                EditTitleBox.Text.Trim(),
                EditTimePicker.Time,
                "无"
            );
        }
        else
        {
            CurrentApp.ScheduleBoardService.Add(
                EditTitleBox.Text.Trim(),
                _editingDate.Value,
                EditTimePicker.Time,
                "无"
            );
        }

        EditOverlay.Visibility = Visibility.Collapsed;
        
        // If we were viewing details, refresh details view
        if (DetailsOverlay.Visibility == Visibility.Visible && _editingDate.HasValue)
        {
            OpenDayDetails(_editingDate.Value);
        }
    }

    private void OnCancelEditClick(object sender, RoutedEventArgs e)
    {
        EditOverlay.Visibility = Visibility.Collapsed;
    }

    private void OnDeleteScheduleClick(object sender, RoutedEventArgs e)
    {
        if (!_editingScheduleId.HasValue) return;
        CurrentApp.ScheduleBoardService.Delete(_editingScheduleId.Value);
        EditOverlay.Visibility = Visibility.Collapsed;
        
        if (DetailsOverlay.Visibility == Visibility.Visible && _editingDate.HasValue)
        {
            OpenDayDetails(_editingDate.Value);
        }
    }
}

public sealed class CalendarDayItem
{
    public string DayNumber { get; set; } = "";
    public DateOnly Date { get; set; }
    public bool IsPlaceholder { get; set; } = false;
    public bool IsToday { get; set; } = false;
    public Brush DayForeground { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 28, 58, 51));
    public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255));
    public Brush BorderBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 225, 235, 231));
    public List<ScheduleItemDisplay> Schedules { get; set; } = new();
}

public sealed class ScheduleItemDisplay
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string DisplayTime { get; set; } = "";
    public Brush ColorBrush { get; set; } = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 13, 93, 86));
}

