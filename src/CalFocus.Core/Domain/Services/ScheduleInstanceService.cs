using CalFocus.Core.Domain.Entities;
using CalFocus.Core.Domain.ValueObjects;

namespace CalFocus.Core.Domain.Services;

/// <summary>
/// 日程实例展开服务，负责将重复规则转换为具体的日程实例
/// </summary>
public sealed class ScheduleInstanceService
{
    /// <summary>
    /// 日程实例，表示一个具体的日程发生
    /// </summary>
    public sealed class ScheduleInstance
    {
        public Guid OriginalScheduleId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTimeOffset StartAt { get; set; }
        public DateTimeOffset EndAt { get; set; }
        public string? Category { get; set; }
        public string? ColorHex { get; set; }
        public string? Note { get; set; }
        public bool IsRecurring { get; set; }
        public DateOnly OccurrenceDate { get; set; }
    }

    /// <summary>
    /// 展开日程为指定日期范围内的所有实例
    /// </summary>
    public IEnumerable<ScheduleInstance> ExpandSchedule(ScheduleItem schedule, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var rule = RecurrenceRule.Parse(schedule.RepeatRule);
        var startDate = DateOnly.FromDateTime(schedule.StartAt.DateTime);
        var occurrenceDates = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd);

        foreach (var occurrenceDate in occurrenceDates)
        {
            var timeOfDay = schedule.StartAt.TimeOfDay;
            var duration = schedule.EndAt - schedule.StartAt;

            yield return new ScheduleInstance
            {
                OriginalScheduleId = schedule.Id,
                Title = schedule.Title,
                StartAt = new DateTimeOffset(occurrenceDate.ToDateTime(TimeOnly.FromTimeSpan(timeOfDay))),
                EndAt = new DateTimeOffset(occurrenceDate.ToDateTime(TimeOnly.FromTimeSpan(timeOfDay))) + duration,
                Category = schedule.Category,
                ColorHex = schedule.ColorHex,
                Note = schedule.Note,
                IsRecurring = rule.Frequency != RecurrenceFrequency.None,
                OccurrenceDate = occurrenceDate
            };
        }
    }

    /// <summary>
    /// 展开多个日程为指定日期范围内的所有实例
    /// </summary>
    public IEnumerable<ScheduleInstance> ExpandSchedules(IEnumerable<ScheduleItem> schedules, DateOnly rangeStart, DateOnly rangeEnd)
    {
        var instances = new List<ScheduleInstance>();

        foreach (var schedule in schedules)
        {
            instances.AddRange(ExpandSchedule(schedule, rangeStart, rangeEnd));
        }

        return instances.OrderBy(x => x.StartAt);
    }

    /// <summary>
    /// 获取指定日期的所有日程实例
    /// </summary>
    public IEnumerable<ScheduleInstance> GetInstancesForDate(IEnumerable<ScheduleItem> schedules, DateOnly date)
    {
        return ExpandSchedules(schedules, date, date);
    }
}
