using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Numerics;

namespace CalFocus.App.Views.Pages;

public sealed partial class CalendarSchedulePage : Page
{
    private const int SideDetailsWidth = 430;
    private const int SideDetailsExpandedWindowWidth = 1560;

    private App CurrentApp => (App)Application.Current;
    private DateTime _currentMonth = DateTime.Now;
    private DateOnly _selectedDate = DateOnly.FromDateTime(DateTime.Now);
    private DateOnly? _focusedDate = DateOnly.FromDateTime(DateTime.Now);
    private bool _isInitializing = true;
    private bool _isSideDetailsVisible;
    private DateOnly? _lastDoubleTapDate;
    private DateTimeOffset _lastDoubleTapAt = DateTimeOffset.MinValue;

    public CalendarSchedulePage()
    {
        InitializeComponent();

        InitializeComboBoxes();

        CurrentApp.ScheduleBoardService.SchedulesChanged += OnSchedulesChanged;
        CurrentApp.ProductivityService.DataChanged += OnProductivityChanged;
        Unloaded += OnPageUnloaded;

        _isInitializing = false;
        RefreshCalendar();
        ApplyDetailsVisibility();
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
        CurrentApp.ProductivityService.DataChanged -= OnProductivityChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void OnSchedulesChanged()
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            RefreshCalendar();
            if (_isSideDetailsVisible)
            {
                RefreshSelectedDateDetails();
            }
        });
    }

    private void OnProductivityChanged()
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            if (_isSideDetailsVisible)
            {
                RefreshSelectedDateDetails();
            }
        });
    }

    private void OnYearMonthSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (YearCombo.SelectedItem is int year && MonthCombo.SelectedItem is int month)
        {
            _currentMonth = new DateTime(year, month, 1);
            EnsureSelectedDateInCurrentMonth();
            EnsureFocusedDateInCurrentMonth();
            RefreshCalendar();
            if (_isSideDetailsVisible)
            {
                RefreshSelectedDateDetails();
            }
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
        var startOffset = (int)firstDay.DayOfWeek;

        var scheduleCountByDate = CurrentApp.ScheduleBoardService
            .GetAll()
            .GroupBy(x => x.Date)
            .ToDictionary(group => group.Key, group => group.Count());

        DaysGrid.Children.Clear();
        var template = (DataTemplate)Resources["DayCellTemplate"];

        var currentDay = 1;
        var started = false;

        var todayBorderBrush = GetAppBrush("AppBrandBrush");
        var selectedBorderBrush = GetAppBrush("AppBrandBrush");
        var normalBorderBrush = GetAppBrush("AppBorderBrush");
        var todayBgBrush = GetAppBrush("AppTodayHighlightBrush");
        var normalBgBrush = GetAppBrush("AppCardBrush");
        var todayBadgeBrush = GetAppBrush("AppTodayHighlightBrush");
        var normalBadgeBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(46, 255, 255, 255));
        var todayTextBrush = GetAppBrush("AppBrandBrush");
        var defaultTextBrush = GetAppBrush("AppTextBrush");

        for (var row = 0; row < 6; row++)
        {
            for (var col = 0; col < 7; col++)
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
                    var isSelected = _focusedDate.HasValue && date == _focusedDate.Value;
                    var count = scheduleCountByDate.TryGetValue(date, out var value) ? value : 0;

                    item = new CalendarDayItem
                    {
                        DayNumber = currentDay.ToString(),
                        Date = date,
                        IsToday = isToday,
                        DayForeground = isToday ? todayTextBrush : defaultTextBrush,
                        BorderBrush = isSelected ? selectedBorderBrush : (isToday ? todayBorderBrush : normalBorderBrush),
                        BackgroundBrush = isToday ? todayBgBrush : normalBgBrush,
                        DayBadgeBackground = isToday ? todayBadgeBrush : normalBadgeBrush,
                        BorderThickness = isSelected ? new Thickness(2.2) : (isToday ? new Thickness(1.6) : new Thickness(1.2)),
                        HoverBorderThickness = isSelected ? new Thickness(2.6) : (isToday ? new Thickness(2.0) : new Thickness(1.8)),
                        ScheduleSummary = count > 0 ? $"{count}项日程" : string.Empty,
                        ScheduleSummaryVisibility = count > 0 ? Visibility.Visible : Visibility.Collapsed
                    };

                    currentDay++;
                }
                else
                {
                    item = new CalendarDayItem
                    {
                        IsPlaceholder = true,
                        DayForeground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0)),
                        BackgroundBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0)),
                        BorderBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0)),
                        DayBadgeBackground = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0, 0, 0, 0)),
                        BorderThickness = new Thickness(0),
                        HoverBorderThickness = new Thickness(0),
                        ScheduleSummaryVisibility = Visibility.Collapsed
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

    private void OnPrevMonthClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(-1);
        EnsureSelectedDateInCurrentMonth();
        EnsureFocusedDateInCurrentMonth();
        RefreshCalendar();
        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private void OnNextMonthClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = _currentMonth.AddMonths(1);
        EnsureSelectedDateInCurrentMonth();
        EnsureFocusedDateInCurrentMonth();
        RefreshCalendar();
        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _currentMonth = DateTime.Now;
        _selectedDate = DateOnly.FromDateTime(DateTime.Now);
        _focusedDate = _selectedDate;
        RefreshCalendar();

        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private void OnAddScheduleClick(object sender, RoutedEventArgs e)
    {
        _selectedDate = _focusedDate ?? DateOnly.FromDateTime(DateTime.Now);
        _focusedDate = _selectedDate;
        RefreshCalendar();
        _ = ShowScheduleEditorAsync(_selectedDate, null);
    }

    private void OnDayCardTapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not CalendarDayItem dayItem)
        {
            return;
        }

        if (_lastDoubleTapDate.HasValue &&
            _lastDoubleTapDate.Value == dayItem.Date &&
            DateTimeOffset.Now - _lastDoubleTapAt <= TimeSpan.FromMilliseconds(360))
        {
            return;
        }

        if (dayItem.IsPlaceholder)
        {
            _focusedDate = null;
            RefreshCalendar();
            return;
        }

        _selectedDate = dayItem.Date;
        _focusedDate = dayItem.Date;
        RefreshCalendar();

        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private async void OnDayCardDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not CalendarDayItem dayItem || dayItem.IsPlaceholder)
        {
            return;
        }

        _lastDoubleTapDate = dayItem.Date;
        _lastDoubleTapAt = DateTimeOffset.Now;

        _selectedDate = dayItem.Date;
        _focusedDate = dayItem.Date;
        RefreshCalendar();

        var pointerInCard = e.GetPosition(border);
        if (IsPointerInsideNamedRegion(border, "DayNumberBadgeBorder", pointerInCard))
        {
            await ShowDayDetailsPopupAsync(dayItem.Date);
            e.Handled = true;
            return;
        }

        await ShowScheduleEditorAsync(dayItem.Date, null);
        e.Handled = true;
    }

    private void OnCalendarBorderTapped(object sender, TappedRoutedEventArgs e)
    {
        if (IsFromNamedElement(e.OriginalSource as DependencyObject, "DayCardBorder"))
        {
            return;
        }

        if (_focusedDate is null)
        {
            return;
        }

        _focusedDate = null;
        RefreshCalendar();
    }

    private void OnShowDetailsClick(object sender, RoutedEventArgs e)
    {
        _selectedDate = _focusedDate ?? _selectedDate;
        _focusedDate ??= _selectedDate;
        var wasVisible = _isSideDetailsVisible;
        _isSideDetailsVisible = true;
        if (!wasVisible)
        {
            EnsureMainWindowExpandedForSideDetails();
        }
        ApplyDetailsVisibility();
        RefreshCalendar();
        RefreshSelectedDateDetails();
    }

    private void OnHideDetailsClick(object sender, RoutedEventArgs e)
    {
        _isSideDetailsVisible = false;
        ApplyDetailsVisibility();
        RefreshCalendar();
    }

    private void ApplyDetailsVisibility()
    {
        DetailsBorder.Visibility = _isSideDetailsVisible ? Visibility.Visible : Visibility.Collapsed;
        DetailColumn.Width = _isSideDetailsVisible
            ? new GridLength(SideDetailsWidth, GridUnitType.Pixel)
            : new GridLength(0, GridUnitType.Pixel);
    }

    private void EnsureMainWindowExpandedForSideDetails()
    {
        _ = CurrentApp.EnsureMainWindowMinWidth(SideDetailsExpandedWindowWidth);
    }

    private void RefreshSelectedDateDetails()
    {
        if (!_isSideDetailsVisible)
        {
            return;
        }

        SelectedDateTitleText.Text = _selectedDate.ToString("yyyy年M月d日");
        SelectedDateHintText.Text = "当日日程、提醒、待办会按时间顺序展示";

        var snapshot = BuildDayDetailsSnapshot(_selectedDate);

        DayScheduleList.ItemsSource = snapshot.Schedules;
        ScheduleDetailCountText.Text = $"共 {snapshot.Schedules.Count} 条";
        EmptyScheduleHintText.Visibility = snapshot.Schedules.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        DayReminderList.ItemsSource = snapshot.Reminders;
        ReminderDetailCountText.Text = $"共 {snapshot.Reminders.Count} 项";
        EmptyReminderHintText.Visibility = snapshot.Reminders.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        DayTodoList.ItemsSource = snapshot.Todos;
        TodoDetailCountText.Text = $"共 {snapshot.Todos.Count} 项";
        EmptyTodoHintText.Visibility = snapshot.Todos.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private DayDetailsSnapshot BuildDayDetailsSnapshot(DateOnly date)
    {
        var dayStart = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue));
        var dayEnd = dayStart.AddDays(1);

        var daySchedules = CurrentApp.ScheduleBoardService
            .GetAll()
            .Where(x => x.Date == date)
            .OrderBy(x => x.StartTime ?? TimeSpan.MaxValue)
            .ThenBy(x => x.Title)
            .Select(x => new ScheduleItemDisplay
            {
                Id = x.Id,
                Title = x.Title,
                DisplayTime = x.StartTime.HasValue
                    ? $"{x.Date:MM-dd} {x.StartTime.Value:hh\\:mm}"
                    : $"{x.Date:MM-dd} 全天"
            })
            .ToList();

        var dayReminders = CurrentApp.ProductivityService
            .GetReminders()
            .Where(x => x.EventAt >= dayStart && x.EventAt < dayEnd)
            .OrderBy(x => x.EventAt)
            .ThenBy(x => x.ReminderAt)
            .Select(x => new ProductivityItemDisplay
            {
                Id = x.Id,
                Title = x.Title,
                DisplaySummary = $"事件 {x.EventAt:HH:mm} · 提醒 {x.ReminderAt:HH:mm}"
            })
            .ToList();

        var dayTodos = CurrentApp.ProductivityService
            .GetTodos(includeCompleted: true)
            .Where(x => x.DueAt >= dayStart && x.DueAt < dayEnd)
            .OrderBy(x => x.DueAt)
            .ThenBy(x => x.Title)
            .Select(x => new ProductivityItemDisplay
            {
                Id = x.Id,
                Title = x.Title,
                DisplaySummary = x.IsCompleted
                    ? $"已完成 · DDL {x.DueAt:HH:mm}"
                    : $"DDL {x.DueAt:HH:mm}"
            })
            .ToList();

        return new DayDetailsSnapshot
        {
            Schedules = daySchedules,
            Reminders = dayReminders,
            Todos = dayTodos
        };
    }

    private void OnAddScheduleForSelectedDateClick(object sender, RoutedEventArgs e)
    {
        _ = ShowScheduleEditorAsync(_selectedDate, null);
    }

    private void OnAddReminderForSelectedDateClick(object sender, RoutedEventArgs e)
    {
        _ = ShowReminderCreatorAsync(_selectedDate);
    }

    private void OnAddTodoForSelectedDateClick(object sender, RoutedEventArgs e)
    {
        _ = ShowTodoCreatorAsync(_selectedDate);
    }

    private void OnScheduleClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        _ = ShowScheduleEditorAsync(_selectedDate, id);
    }

    private async Task ShowScheduleEditorAsync(DateOnly date, Guid? scheduleId)
    {
        var existing = scheduleId.HasValue
            ? CurrentApp.ScheduleBoardService.GetAll().FirstOrDefault(x => x.Id == scheduleId.Value)
            : null;

        var titleBox = new TextBox
        {
            Header = "日程标题",
            PlaceholderText = "输入事件名称",
            Text = existing?.Title ?? string.Empty
        };
        var timePicker = new TimePicker
        {
            Header = "开始时间",
            Time = existing?.StartTime ?? new TimeSpan(9, 0, 0)
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = date.ToString("yyyy年M月d日"),
                    Foreground = GetAppBrush("AppMutedBrush")
                },
                titleBox,
                timePicker
            }
        };

        var dialog = new ContentDialog
        {
            Title = scheduleId.HasValue ? "编辑日程" : "新建日程",
            Content = panel,
            PrimaryButtonText = "保存",
            CloseButtonText = "取消",
            SecondaryButtonText = scheduleId.HasValue ? "删除" : string.Empty,
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            if (string.IsNullOrWhiteSpace(titleBox.Text))
            {
                return;
            }

            if (scheduleId.HasValue)
            {
                CurrentApp.ScheduleBoardService.Update(scheduleId.Value, titleBox.Text.Trim(), timePicker.Time, "无");
            }
            else
            {
                CurrentApp.ScheduleBoardService.Add(titleBox.Text.Trim(), date, timePicker.Time, "无");
            }

            _selectedDate = date;
            _focusedDate = date;
            RefreshCalendar();
            if (_isSideDetailsVisible)
            {
                RefreshSelectedDateDetails();
            }
            return;
        }

        if (result == ContentDialogResult.Secondary && scheduleId.HasValue)
        {
            CurrentApp.ScheduleBoardService.Delete(scheduleId.Value);
            RefreshCalendar();
            if (_isSideDetailsVisible)
            {
                RefreshSelectedDateDetails();
            }
        }
    }

    private async Task ShowReminderCreatorAsync(DateOnly date)
    {
        var titleBox = new TextBox
        {
            Header = "提醒标题",
            PlaceholderText = "例如：项目站会"
        };

        var eventTimePicker = new TimePicker
        {
            Header = "事件时间",
            Time = new TimeSpan(9, 0, 0)
        };

        var reminderTimePicker = new TimePicker
        {
            Header = "提醒时间",
            Time = new TimeSpan(8, 50, 0)
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = date.ToString("yyyy年M月d日"),
                    Foreground = GetAppBrush("AppMutedBrush")
                },
                titleBox,
                eventTimePicker,
                reminderTimePicker
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

        var eventAt = new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(eventTimePicker.Time)));
        var reminderAt = new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(reminderTimePicker.Time)));

        CurrentApp.ProductivityService.AddReminder(titleBox.Text.Trim(), eventAt, reminderAt);
        _selectedDate = date;
        _focusedDate = date;
        RefreshCalendar();
        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private async Task ShowTodoCreatorAsync(DateOnly date)
    {
        var titleBox = new TextBox
        {
            Header = "待办名称",
            PlaceholderText = "输入待办标题"
        };

        var dueTimePicker = new TimePicker
        {
            Header = "DDL 时间",
            Time = new TimeSpan(18, 0, 0)
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = date.ToString("yyyy年M月d日"),
                    Foreground = GetAppBrush("AppMutedBrush")
                },
                titleBox,
                dueTimePicker
            }
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

        var dueAt = new DateTimeOffset(date.ToDateTime(TimeOnly.FromTimeSpan(dueTimePicker.Time)));
        CurrentApp.ProductivityService.AddTodo(titleBox.Text.Trim(), dueAt);
        _selectedDate = date;
        _focusedDate = date;
        RefreshCalendar();
        if (_isSideDetailsVisible)
        {
            RefreshSelectedDateDetails();
        }
    }

    private async Task ShowDayDetailsPopupAsync(DateOnly date)
    {
        var snapshot = BuildDayDetailsSnapshot(date);

        var root = new StackPanel
        {
            Spacing = 10
        };

        root.Children.Add(CreatePopupSection("日程", snapshot.Schedules.Select(x => $"{x.DisplayTime} · {x.Title}")));
        root.Children.Add(CreatePopupSection("提醒", snapshot.Reminders.Select(x => $"{x.Title} · {x.DisplaySummary}")));
        root.Children.Add(CreatePopupSection("待办", snapshot.Todos.Select(x => $"{x.Title} · {x.DisplaySummary}")));

        var dialog = new ContentDialog
        {
            Title = $"{date:yyyy年M月d日} 详情",
            Content = new Border
            {
                CornerRadius = new CornerRadius(16),
                BorderBrush = GetAppBrush("AppBorderBrush"),
                BorderThickness = new Thickness(1),
                Background = GetAppBrush("AppGlassBrush"),
                Padding = new Thickness(12),
                Child = new ScrollViewer
                {
                    MaxHeight = 540,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = root
                }
            },
            CloseButtonText = "关闭",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private Border CreatePopupSection(string title, IEnumerable<string> lines)
    {
        var lineList = lines.ToList();

        var stack = new StackPanel
        {
            Spacing = 4,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Foreground = GetAppBrush("AppTitleBrush")
                }
            }
        };

        if (lineList.Count == 0)
        {
            stack.Children.Add(new TextBlock
            {
                Text = "当天暂无内容",
                Opacity = 0.65,
                Foreground = GetAppBrush("AppMutedBrush"),
                TextWrapping = TextWrapping.Wrap
            });
        }
        else
        {
            foreach (var line in lineList.Take(8))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "• " + line,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0.92,
                    Foreground = GetAppBrush("AppTextBrush")
                });
            }
        }

        return new Border
        {
            CornerRadius = new CornerRadius(16),
            BorderBrush = GetAppBrush("AppBorderBrush"),
            BorderThickness = new Thickness(1),
            Background = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(170, 246, 252, 249)),
            Padding = new Thickness(12),
            Child = stack
        };
    }

    private void OnDayCardPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (!CurrentApp.UiSettingsService.Current.EnableHoverFeedback)
        {
            return;
        }

        if (sender is not Border border || border.DataContext is not CalendarDayItem dayItem || dayItem.IsPlaceholder)
        {
            return;
        }

        border.Translation = new Vector3(0, -2, 0);
        border.BorderThickness = dayItem.HoverBorderThickness;
    }

    private void OnDayCardPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not Border border || border.DataContext is not CalendarDayItem dayItem || dayItem.IsPlaceholder)
        {
            return;
        }

        border.Translation = new Vector3(0, 0, 0);
        border.BorderThickness = dayItem.BorderThickness;
    }

    private Brush GetAppBrush(string key)
    {
        if (Application.Current.Resources.TryGetValue(key, out var value) && value is Brush brush)
        {
            return brush;
        }

        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private void EnsureSelectedDateInCurrentMonth()
    {
        if (_selectedDate.Year == _currentMonth.Year && _selectedDate.Month == _currentMonth.Month)
        {
            return;
        }

        var day = Math.Min(_selectedDate.Day, DateTime.DaysInMonth(_currentMonth.Year, _currentMonth.Month));
        _selectedDate = new DateOnly(_currentMonth.Year, _currentMonth.Month, day);
    }

    private void EnsureFocusedDateInCurrentMonth()
    {
        if (_focusedDate is null)
        {
            return;
        }

        if (_focusedDate.Value.Year == _currentMonth.Year && _focusedDate.Value.Month == _currentMonth.Month)
        {
            return;
        }

        _focusedDate = null;
    }

    private static bool TryGetDateFromTag(object? tag, out DateOnly date)
    {
        if (tag is DateOnly dateOnly && dateOnly.Year >= 1900)
        {
            date = dateOnly;
            return true;
        }

        if (tag is DateTime dateTime)
        {
            date = DateOnly.FromDateTime(dateTime);
            return true;
        }

        if (tag is DateTimeOffset dateTimeOffset)
        {
            date = DateOnly.FromDateTime(dateTimeOffset.DateTime);
            return true;
        }

        date = default;
        return false;
    }

    private static bool IsFromNamedElement(DependencyObject? source, string targetName)
    {
        var current = source;

        while (current is not null)
        {
            if (current is FrameworkElement element && string.Equals(element.Name, targetName, StringComparison.Ordinal))
            {
                return true;
            }

            current = VisualTreeHelper.GetParent(current);
        }

        return false;
    }

    private static bool IsPointerInsideNamedRegion(FrameworkElement root, string targetName, Windows.Foundation.Point pointerInRoot)
    {
        if (root.FindName(targetName) is not FrameworkElement target ||
            target.ActualWidth <= 0 ||
            target.ActualHeight <= 0)
        {
            return false;
        }

        var transform = target.TransformToVisual(root);
        var topLeft = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
        var right = topLeft.X + target.ActualWidth;
        var bottom = topLeft.Y + target.ActualHeight;

        return pointerInRoot.X >= topLeft.X &&
               pointerInRoot.X <= right &&
               pointerInRoot.Y >= topLeft.Y &&
               pointerInRoot.Y <= bottom;
    }
}

public sealed class CalendarDayItem
{
    public string DayNumber { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public bool IsPlaceholder { get; set; }
    public bool IsToday { get; set; }
    public Brush DayForeground { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.Black);
    public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.White);
    public Brush BorderBrush { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.White);
    public Thickness BorderThickness { get; set; } = new Thickness(1.2);
    public Thickness HoverBorderThickness { get; set; } = new Thickness(1.8);
    public Brush DayBadgeBackground { get; set; } = new SolidColorBrush(Microsoft.UI.Colors.White);
    public string ScheduleSummary { get; set; } = string.Empty;
    public Visibility ScheduleSummaryVisibility { get; set; } = Visibility.Collapsed;
}

public sealed class ScheduleItemDisplay
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DisplayTime { get; set; } = string.Empty;
}

public sealed class ProductivityItemDisplay
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DisplaySummary { get; set; } = string.Empty;
}

public sealed class DayDetailsSnapshot
{
    public List<ScheduleItemDisplay> Schedules { get; set; } = new();
    public List<ProductivityItemDisplay> Reminders { get; set; } = new();
    public List<ProductivityItemDisplay> Todos { get; set; } = new();
}
