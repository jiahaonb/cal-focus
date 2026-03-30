using CalFocus.Core.Domain.Entities;
using CalFocus.Core.Domain.Services;

namespace CalFocus.Core.Tests.Domain.Services;

public class ScheduleInstanceServiceTests
{
    private readonly ScheduleInstanceService _service = new();

    [Fact]
    public void ExpandSchedule_WithNonRecurringSchedule_ReturnsSingleInstance()
    {
        var schedule = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            Title = "一次性会议",
            StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 9, 30, 0)),
            EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 10, 30, 0)),
            RepeatRule = "无"
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 31);

        var instances = _service.ExpandSchedule(schedule, rangeStart, rangeEnd).ToList();

        Assert.Single(instances);
        Assert.Equal("一次性会议", instances[0].Title);
        Assert.Equal(new DateOnly(2026, 3, 26), instances[0].OccurrenceDate);
    }

    [Fact]
    public void ExpandSchedule_WithDailyRecurrence_ReturnsMultipleInstances()
    {
        var schedule = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            Title = "每日站会",
            StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 9, 30, 0)),
            EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 10, 0, 0)),
            RepeatRule = "每天"
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 30);

        var instances = _service.ExpandSchedule(schedule, rangeStart, rangeEnd).ToList();

        Assert.Equal(5, instances.Count);
        Assert.All(instances, x => Assert.Equal("每日站会", x.Title));
        Assert.True(instances.All(x => x.IsRecurring));
    }

    [Fact]
    public void ExpandSchedule_WithWeeklyRecurrence_ReturnsWeeklyInstances()
    {
        var schedule = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            Title = "周会",
            StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 14, 0, 0)),
            EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 15, 0, 0)),
            RepeatRule = "每周"
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 4, 30);

        var instances = _service.ExpandSchedule(schedule, rangeStart, rangeEnd).ToList();

        Assert.Equal(5, instances.Count);
        Assert.Equal(new DateOnly(2026, 3, 26), instances[0].OccurrenceDate);
        Assert.Equal(new DateOnly(2026, 4, 2), instances[1].OccurrenceDate);
        Assert.Equal(new DateOnly(2026, 4, 9), instances[2].OccurrenceDate);
    }

    [Fact]
    public void ExpandSchedule_PreservesTimeOfDay()
    {
        var schedule = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            Title = "测试",
            StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 14, 30, 0)),
            EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 15, 30, 0)),
            RepeatRule = "每天"
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 28);

        var instances = _service.ExpandSchedule(schedule, rangeStart, rangeEnd).ToList();

        Assert.All(instances, x =>
        {
            Assert.Equal(14, x.StartAt.Hour);
            Assert.Equal(30, x.StartAt.Minute);
        });
    }

    [Fact]
    public void ExpandSchedule_PreservesDuration()
    {
        var schedule = new ScheduleItem
        {
            Id = Guid.NewGuid(),
            Title = "测试",
            StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 9, 0, 0)),
            EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 11, 30, 0)),
            RepeatRule = "每天"
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 28);

        var instances = _service.ExpandSchedule(schedule, rangeStart, rangeEnd).ToList();

        var expectedDuration = TimeSpan.FromHours(2.5);
        Assert.All(instances, x =>
        {
            Assert.Equal(expectedDuration, x.EndAt - x.StartAt);
        });
    }

    [Fact]
    public void ExpandSchedules_ReturnsOrderedInstances()
    {
        var schedules = new[]
        {
            new ScheduleItem
            {
                Id = Guid.NewGuid(),
                Title = "会议 A",
                StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 14, 0, 0)),
                EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 15, 0, 0)),
                RepeatRule = "无"
            },
            new ScheduleItem
            {
                Id = Guid.NewGuid(),
                Title = "会议 B",
                StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 9, 0, 0)),
                EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 10, 0, 0)),
                RepeatRule = "无"
            }
        };

        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 26);

        var instances = _service.ExpandSchedules(schedules, rangeStart, rangeEnd).ToList();

        Assert.Equal(2, instances.Count);
        Assert.Equal("会议 B", instances[0].Title); // 应该按时间排序
        Assert.Equal("会议 A", instances[1].Title);
    }

    [Fact]
    public void GetInstancesForDate_ReturnsOnlyInstancesForSpecificDate()
    {
        var schedules = new[]
        {
            new ScheduleItem
            {
                Id = Guid.NewGuid(),
                Title = "每日会议",
                StartAt = new DateTimeOffset(new DateTime(2026, 3, 26, 9, 0, 0)),
                EndAt = new DateTimeOffset(new DateTime(2026, 3, 26, 10, 0, 0)),
                RepeatRule = "每天"
            }
        };

        var targetDate = new DateOnly(2026, 3, 28);
        var instances = _service.GetInstancesForDate(schedules, targetDate).ToList();

        Assert.Single(instances);
        Assert.Equal(targetDate, instances[0].OccurrenceDate);
    }
}
