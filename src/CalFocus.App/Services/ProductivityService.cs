using System.Text.Json;
using CalFocus.Core.Abstractions.Services;

namespace CalFocus.App.Services;

public sealed class ProductivityService : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _dataPath;
    private readonly DispatcherTimer _reminderTimer;

    private ProductivityState _state = new();

    public event Action? DataChanged;
    public event Action<ReminderTaskItem>? ReminderTriggered;

    public ProductivityService(IAppDataPathService pathService)
    {
        _dataPath = pathService.ProductivityDataPath;
        Load();

        _reminderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(15)
        };
        _reminderTimer.Tick += (_, _) => ProcessReminders();
        _reminderTimer.Start();
    }

    public IReadOnlyList<TodoTaskItem> GetTodos(bool includeCompleted = false)
    {
        var items = _state.Todos
            .Where(x => includeCompleted || !x.IsCompleted)
            .OrderBy(x => x.DueAt)
            .ThenBy(x => x.Title)
            .ToList();

        return items;
    }

    public IReadOnlyList<ReminderTaskItem> GetReminders()
    {
        return _state.Reminders
            .OrderBy(x => x.ReminderAt)
            .ThenBy(x => x.Title)
            .ToList();
    }

    public TodoTaskItem AddTodo(string title, DateTimeOffset dueAt, string? colorHex = null)
    {
        var item = new TodoTaskItem
        {
            Title = title.Trim(),
            DueAt = dueAt,
            ColorHex = string.IsNullOrWhiteSpace(colorHex) ? "#E4F4EE" : colorHex.Trim()
        };

        _state.Todos.Add(item);
        SaveAndNotify();
        return item;
    }

    public bool ToggleTodo(Guid id)
    {
        var item = _state.Todos.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        item.IsCompleted = !item.IsCompleted;
        SaveAndNotify();
        return true;
    }

    public bool DeleteTodo(Guid id)
    {
        var item = _state.Todos.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _state.Todos.Remove(item);
        SaveAndNotify();
        return true;
    }

    public ReminderTaskItem AddReminder(string title, DateTimeOffset eventAt, DateTimeOffset reminderAt)
    {
        var item = new ReminderTaskItem
        {
            Title = title.Trim(),
            EventAt = eventAt,
            ReminderAt = reminderAt,
            IsNotified = false
        };

        _state.Reminders.Add(item);
        SaveAndNotify();
        return item;
    }

    public bool UpdateReminder(Guid id, string title, DateTimeOffset eventAt, DateTimeOffset reminderAt)
    {
        var item = _state.Reminders.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        item.Title = title.Trim();
        item.EventAt = eventAt;
        item.ReminderAt = reminderAt;
        item.IsNotified = false;

        SaveAndNotify();
        return true;
    }

    public bool DeleteReminder(Guid id)
    {
        var item = _state.Reminders.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return false;
        }

        _state.Reminders.Remove(item);
        SaveAndNotify();
        return true;
    }

    public void Dispose()
    {
        _reminderTimer.Stop();
    }

    private void ProcessReminders()
    {
        var now = DateTimeOffset.Now;
        var dueItems = _state.Reminders
            .Where(x => !x.IsNotified && x.ReminderAt <= now)
            .OrderBy(x => x.ReminderAt)
            .ToList();

        if (dueItems.Count == 0)
        {
            return;
        }

        foreach (var item in dueItems)
        {
            item.IsNotified = true;
            ReminderTriggered?.Invoke(item);
        }

        SaveAndNotify();
    }

    private void Load()
    {
        if (!File.Exists(_dataPath))
        {
            SeedDefaults();
            SaveInternal();
            return;
        }

        try
        {
            var json = File.ReadAllText(_dataPath);
            _state = JsonSerializer.Deserialize<ProductivityState>(json, JsonOptions) ?? new ProductivityState();

            _state.Todos ??= new List<TodoTaskItem>();
            _state.Reminders ??= new List<ReminderTaskItem>();
        }
        catch
        {
            _state = new ProductivityState();
            SeedDefaults();
            SaveInternal();
        }
    }

    private void SaveAndNotify()
    {
        SaveInternal();
        DataChanged?.Invoke();
    }

    private void SaveInternal()
    {
        var directory = Path.GetDirectoryName(_dataPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(_state, JsonOptions);
        File.WriteAllText(_dataPath, json);
    }

    private void SeedDefaults()
    {
        var now = DateTimeOffset.Now;

        _state.Todos = new List<TodoTaskItem>
        {
            new()
            {
                Title = "完成本周冲刺复盘",
                DueAt = now.Date.AddHours(18),
                ColorHex = "#E8F5EE"
            },
            new()
            {
                Title = "整理下周会议材料",
                DueAt = now.Date.AddDays(1).AddHours(11),
                ColorHex = "#EAF2FF"
            }
        };

        _state.Reminders = new List<ReminderTaskItem>
        {
            new()
            {
                Title = "站会提醒",
                EventAt = now.Date.AddHours(9).AddMinutes(30),
                ReminderAt = now.Date.AddHours(9).AddMinutes(20)
            }
        };
    }

    private sealed class ProductivityState
    {
        public List<TodoTaskItem> Todos { get; set; } = new();
        public List<ReminderTaskItem> Reminders { get; set; } = new();
    }
}

public sealed class TodoTaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset DueAt { get; set; } = DateTimeOffset.Now;
    public string ColorHex { get; set; } = "#E8F5EE";
    public bool IsCompleted { get; set; }

    public string DisplayDdl => $"DDL {DueAt:MM-dd HH:mm}";
}

public sealed class ReminderTaskItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset EventAt { get; set; } = DateTimeOffset.Now;
    public DateTimeOffset ReminderAt { get; set; } = DateTimeOffset.Now;
    public bool IsNotified { get; set; }

    public string DisplayEventDate => $"事件: {EventAt:yyyy-MM-dd HH:mm}";
    public string DisplayReminderDate => $"提醒: {ReminderAt:yyyy-MM-dd HH:mm}";
}
