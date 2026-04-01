using CalFocus.App.Services;
using Microsoft.UI.Xaml.Media;

namespace CalFocus.App.Views.Pages;

public sealed partial class TodoReminderPage : Page
{
    private App CurrentApp => (App)Application.Current;

    public TodoReminderPage()
    {
        InitializeComponent();

        CurrentApp.ProductivityService.DataChanged += OnProductivityDataChanged;
        Unloaded += OnPageUnloaded;

        RefreshTodos();
        RefreshReminders();
    }

    private void OnProductivityDataChanged()
    {
        _ = DispatcherQueue.TryEnqueue(() =>
        {
            RefreshTodos();
            RefreshReminders();
        });
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        CurrentApp.ProductivityService.DataChanged -= OnProductivityDataChanged;
        Unloaded -= OnPageUnloaded;
    }

    private void RefreshTodos()
    {
        var todos = CurrentApp.ProductivityService
            .GetTodos(includeCompleted: true)
            .Select(x => new TodoReminderTodoCard
            {
                Id = x.Id,
                Title = x.IsCompleted ? $"[已完成] {x.Title}" : x.Title,
                DisplayDdl = x.DisplayDdl,
                BackgroundBrush = CreateBrushFromHex(x.ColorHex)
            })
            .ToList();

        TodoList.ItemsSource = todos;
        TodoCountText.Text = $"共 {todos.Count} 项";
    }

    private void RefreshReminders()
    {
        var reminders = CurrentApp.ProductivityService.GetReminders().ToList();
        ReminderList.ItemsSource = reminders;
        ReminderCountText.Text = $"共 {reminders.Count} 项";
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
        await ShowReminderEditorAsync(null);
    }

    private void OnCompleteTodoClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        CurrentApp.ProductivityService.ToggleTodo(id);
    }

    private void OnDeleteTodoClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        CurrentApp.ProductivityService.DeleteTodo(id);
    }

    private async void OnEditReminderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        await ShowReminderEditorAsync(id);
    }

    private void OnDeleteReminderClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not Guid id)
        {
            return;
        }

        CurrentApp.ProductivityService.DeleteReminder(id);
    }

    private async Task ShowReminderEditorAsync(Guid? reminderId)
    {
        var existing = reminderId.HasValue
            ? CurrentApp.ProductivityService.GetReminders().FirstOrDefault(x => x.Id == reminderId.Value)
            : null;

        var titleBox = new TextBox
        {
            Header = "提醒标题",
            PlaceholderText = "例如：产品评审会议",
            Text = existing?.Title ?? string.Empty
        };

        var eventDatePicker = new DatePicker
        {
            Header = "事件日期",
            Date = existing?.EventAt ?? DateTimeOffset.Now.Date
        };
        var eventTimePicker = new TimePicker
        {
            Header = "事件时间",
            Time = existing?.EventAt.TimeOfDay ?? new TimeSpan(9, 0, 0)
        };

        var reminderDatePicker = new DatePicker
        {
            Header = "提醒日期",
            Date = existing?.ReminderAt ?? DateTimeOffset.Now.Date
        };
        var reminderTimePicker = new TimePicker
        {
            Header = "提醒时间",
            Time = existing?.ReminderAt.TimeOfDay ?? DateTime.Now.AddMinutes(30).TimeOfDay
        };

        var panel = new StackPanel
        {
            Spacing = 10,
            Children =
            {
                titleBox,
                eventDatePicker,
                eventTimePicker,
                reminderDatePicker,
                reminderTimePicker
            }
        };

        var dialog = new ContentDialog
        {
            Title = reminderId.HasValue ? "编辑提醒" : "新增提醒",
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
        var reminderAt = reminderDatePicker.Date.Date + reminderTimePicker.Time;

        if (reminderId.HasValue)
        {
            CurrentApp.ProductivityService.UpdateReminder(reminderId.Value, titleBox.Text, eventAt, reminderAt);
            return;
        }

        CurrentApp.ProductivityService.AddReminder(titleBox.Text, eventAt, reminderAt);
    }

    private static Brush CreateBrushFromHex(string? hex)
    {
        var fallback = Microsoft.UI.ColorHelper.FromArgb(255, 232, 245, 238);
        if (string.IsNullOrWhiteSpace(hex))
        {
            return new SolidColorBrush(fallback);
        }

        var normalized = hex.Trim().TrimStart('#');
        if (normalized.Length != 6)
        {
            return new SolidColorBrush(fallback);
        }

        try
        {
            var r = Convert.ToByte(normalized.Substring(0, 2), 16);
            var g = Convert.ToByte(normalized.Substring(2, 2), 16);
            var b = Convert.ToByte(normalized.Substring(4, 2), 16);
            return new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, r, g, b));
        }
        catch
        {
            return new SolidColorBrush(fallback);
        }
    }
}

public sealed class TodoReminderTodoCard
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string DisplayDdl { get; set; } = string.Empty;
    public Brush BackgroundBrush { get; set; } = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(255, 232, 245, 238));
}
