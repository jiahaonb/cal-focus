using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;
using CalFocus.Core.Domain.Services;
using CalFocus.Core.Domain.ValueObjects;

namespace CalFocus.App.Services;

public sealed class ScheduleBoardService
{
    private readonly IScheduleRepository _repository;
    private readonly ScheduleInstanceService _instanceService;
    private List<ScheduleItem>? _cachedSchedules;
    private DateTime _cacheTime = DateTime.MinValue;
    private const int CacheDurationMs = 5000; // 5 秒缓存

    public event Action? SchedulesChanged;

    public ScheduleBoardService(IScheduleRepository repository)
    {
        _repository = repository;
        _instanceService = new ScheduleInstanceService();
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await SeedIfEmptyAsync();
    }

    public IReadOnlyList<ScheduleEntry> GetAll()
    {
        var schedules = GetSchedulesSync();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var rangeEnd = today.AddDays(90); // 获取未来 90 天的日程

        var instances = _instanceService.ExpandSchedules(schedules, today, rangeEnd);
        return instances
            .Select(x => new ScheduleEntry
            {
                Id = x.OriginalScheduleId,
                Title = x.Title,
                Date = x.OccurrenceDate,
                StartTime = x.StartAt.TimeOfDay,
                RepeatRule = x.IsRecurring ? GetRepeatRuleText(x.OriginalScheduleId) : "无"
            })
            .OrderBy(x => x.Date)
            .ThenBy(x => x.StartTime ?? TimeSpan.MaxValue)
            .ToList();
    }

    public IReadOnlyList<ScheduleEntry> GetByDate(DateOnly date)
    {
        var schedules = GetSchedulesSync();
        var instances = _instanceService.GetInstancesForDate(schedules, date);

        return instances
            .Select(x => new ScheduleEntry
            {
                Id = x.OriginalScheduleId,
                Title = x.Title,
                Date = x.OccurrenceDate,
                StartTime = x.StartAt.TimeOfDay,
                RepeatRule = x.IsRecurring ? GetRepeatRuleText(x.OriginalScheduleId) : "无"
            })
            .OrderBy(x => x.StartTime ?? TimeSpan.MaxValue)
            .ToList();
    }

    public void Add(string title, DateOnly date, TimeSpan? startTime, string repeatRule)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var timeOfDay = startTime.HasValue ? TimeOnly.FromTimeSpan(startTime.Value) : TimeOnly.MinValue;
        var startAt = new DateTimeOffset(date.ToDateTime(timeOfDay));
        var endAt = startAt.AddHours(1); // 默认 1 小时时长

        var item = new ScheduleItem
        {
            Title = title.Trim(),
            StartAt = startAt,
            EndAt = endAt,
            RepeatRule = NormalizeRepeatRule(repeatRule)
        };

        _ = AddAsync(item);
    }

    public void Update(Guid id, string title, TimeSpan? startTime, string repeatRule)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var schedules = GetSchedulesSync();
        var item = schedules.FirstOrDefault(x => x.Id == id);
        if (item is null)
        {
            return;
        }

        var date = DateOnly.FromDateTime(item.StartAt.DateTime);
        var timeOfDay = startTime.HasValue ? TimeOnly.FromTimeSpan(startTime.Value) : TimeOnly.MinValue;
        var startAt = new DateTimeOffset(date.ToDateTime(timeOfDay));
        var duration = item.EndAt - item.StartAt;

        item.Title = title.Trim();
        item.StartAt = startAt;
        item.EndAt = startAt + duration;
        item.RepeatRule = NormalizeRepeatRule(repeatRule);

        _ = UpdateAsync(item);
    }

    public void Delete(Guid id)
    {
        _ = DeleteAsync(id);
    }

    private async Task AddAsync(ScheduleItem item)
    {
        await _repository.AddAsync(item);
        InvalidateCache();
        SchedulesChanged?.Invoke();
    }

    private async Task UpdateAsync(ScheduleItem item)
    {
        await _repository.UpdateAsync(item);
        InvalidateCache();
        SchedulesChanged?.Invoke();
    }

    private async Task DeleteAsync(Guid id)
    {
        await _repository.DeleteAsync(id);
        InvalidateCache();
        SchedulesChanged?.Invoke();
    }

    private List<ScheduleItem> GetSchedulesSync()
    {
        var now = DateTime.Now;
        if (_cachedSchedules != null && (now - _cacheTime).TotalMilliseconds < CacheDurationMs)
        {
            return _cachedSchedules;
        }

        // 同步获取（应该改为异步，但为了兼容现有 UI 代码）
        _cachedSchedules = _repository.GetAllAsync().GetAwaiter().GetResult().ToList();
        _cacheTime = now;
        return _cachedSchedules;
    }

    private void InvalidateCache()
    {
        _cachedSchedules = null;
        _cacheTime = DateTime.MinValue;
    }

    private string GetRepeatRuleText(Guid scheduleId)
    {
        var schedules = GetSchedulesSync();
        var item = schedules.FirstOrDefault(x => x.Id == scheduleId);
        return item?.RepeatRule ?? "无";
    }

    private static string NormalizeRepeatRule(string? repeatRule)
    {
        return repeatRule switch
        {
            "每天" => "每天",
            "每周" => "每周",
            "每月" => "每月",
            _ => "无"
        };
    }

    private async Task SeedIfEmptyAsync()
    {
        var existing = await _repository.GetAllAsync();
        if (existing.Count > 0)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var seedItems = new[]
        {
            new ScheduleItem
            {
                Title = "项目站会",
                StartAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(9, 30, 0))),
                EndAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(10, 30, 0))),
                RepeatRule = "每周"
            },
            new ScheduleItem
            {
                Title = "交互评审",
                StartAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(14, 0, 0))),
                EndAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(15, 0, 0))),
                RepeatRule = "无"
            },
            new ScheduleItem
            {
                Title = "每日复盘",
                StartAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(17, 30, 0))),
                EndAt = new DateTimeOffset(today.ToDateTime(new TimeOnly(18, 0, 0))),
                RepeatRule = "每天"
            }
        };

        foreach (var item in seedItems)
        {
            await _repository.AddAsync(item);
        }

        InvalidateCache();
    }
}

public sealed class ScheduleEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeSpan? StartTime { get; set; }
    public string RepeatRule { get; set; } = "无";

    public string DisplayDateText => Date.ToString("yyyy-MM-dd");
    public string DisplayTimeText => StartTime.HasValue ? StartTime.Value.ToString(@"hh\:mm") : string.Empty;
    public string DisplayWhen => StartTime.HasValue
        ? $"{Date:yyyy-MM-dd} {StartTime.Value:hh\\:mm}"
        : $"{Date:yyyy-MM-dd}";

    public string DisplayRepeat => RepeatRule == "无" ? "" : $"重复：{RepeatRule}";

    public string DisplayRepeatBadge => RepeatRule switch
    {
        "无" => "",
        "每天" => "🔄 每天",
        "每周" => "🔄 每周",
        "每月" => "🔄 每月",
        _ => ""
    };
}
