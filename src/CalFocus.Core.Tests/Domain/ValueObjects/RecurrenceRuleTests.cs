using CalFocus.Core.Domain.ValueObjects;

namespace CalFocus.Core.Tests.Domain.ValueObjects;

public class RecurrenceRuleTests
{
    [Fact]
    public void Parse_WithNone_ReturnsNoneFrequency()
    {
        var rule = RecurrenceRule.Parse("无");
        Assert.Equal(RecurrenceFrequency.None, rule.Frequency);
    }

    [Fact]
    public void Parse_WithDaily_ReturnsDailyFrequency()
    {
        var rule = RecurrenceRule.Parse("每天");
        Assert.Equal(RecurrenceFrequency.Daily, rule.Frequency);
    }

    [Fact]
    public void Parse_WithWeekly_ReturnsWeeklyFrequency()
    {
        var rule = RecurrenceRule.Parse("每周");
        Assert.Equal(RecurrenceFrequency.Weekly, rule.Frequency);
    }

    [Fact]
    public void Parse_WithMonthly_ReturnsMonthlyFrequency()
    {
        var rule = RecurrenceRule.Parse("每月");
        Assert.Equal(RecurrenceFrequency.Monthly, rule.Frequency);
    }

    [Fact]
    public void GenerateOccurrences_WithNone_ReturnsSingleDate()
    {
        var rule = RecurrenceRule.None();
        var startDate = new DateOnly(2026, 3, 26);
        var rangeStart = new DateOnly(2026, 3, 20);
        var rangeEnd = new DateOnly(2026, 3, 31);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Single(occurrences);
        Assert.Equal(startDate, occurrences[0]);
    }

    [Fact]
    public void GenerateOccurrences_WithDaily_ReturnsMultipleDates()
    {
        var rule = RecurrenceRule.Daily();
        var startDate = new DateOnly(2026, 3, 26);
        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 30);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Equal(5, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 3, 26), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 3, 30), occurrences[4]);
    }

    [Fact]
    public void GenerateOccurrences_WithWeekly_ReturnsWeeklyDates()
    {
        var rule = RecurrenceRule.Weekly();
        var startDate = new DateOnly(2026, 3, 26); // Thursday
        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 4, 30);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Equal(5, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 3, 26), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 4, 2), occurrences[1]);
        Assert.Equal(new DateOnly(2026, 4, 9), occurrences[2]);
    }

    [Fact]
    public void GenerateOccurrences_WithMonthly_ReturnsMonthlyDates()
    {
        var rule = RecurrenceRule.Monthly();
        var startDate = new DateOnly(2026, 3, 26);
        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 8, 31);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Equal(6, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 3, 26), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 4, 26), occurrences[1]);
        Assert.Equal(new DateOnly(2026, 5, 26), occurrences[2]);
    }

    [Fact]
    public void GenerateOccurrences_WithMonthly_HandlesMonthEndCorrectly()
    {
        var rule = RecurrenceRule.Monthly();
        var startDate = new DateOnly(2026, 1, 31); // January 31st
        var rangeStart = new DateOnly(2026, 1, 31);
        var rangeEnd = new DateOnly(2026, 4, 30);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        // February should be 28th (not 31st)
        Assert.Equal(new DateOnly(2026, 1, 31), occurrences[0]);
        Assert.Equal(new DateOnly(2026, 2, 28), occurrences[1]);
        Assert.Equal(new DateOnly(2026, 3, 31), occurrences[2]);
        Assert.Equal(new DateOnly(2026, 4, 30), occurrences[3]);
    }

    [Fact]
    public void GenerateOccurrences_WithMaxOccurrences_StopsAtLimit()
    {
        var rule = RecurrenceRule.Daily(maxOccurrences: 3);
        var startDate = new DateOnly(2026, 3, 26);
        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 31);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Equal(3, occurrences.Count);
    }

    [Fact]
    public void GenerateOccurrences_WithEndDate_StopsAtEndDate()
    {
        var rule = RecurrenceRule.Daily(endDate: new DateOnly(2026, 3, 28));
        var startDate = new DateOnly(2026, 3, 26);
        var rangeStart = new DateOnly(2026, 3, 26);
        var rangeEnd = new DateOnly(2026, 3, 31);

        var occurrences = rule.GenerateOccurrences(startDate, rangeStart, rangeEnd).ToList();

        Assert.Equal(3, occurrences.Count);
        Assert.Equal(new DateOnly(2026, 3, 28), occurrences[2]);
    }

    [Fact]
    public void ToDisplayText_ReturnsCorrectText()
    {
        Assert.Equal("无", RecurrenceRule.None().ToDisplayText());
        Assert.Equal("每天", RecurrenceRule.Daily().ToDisplayText());
        Assert.Equal("每周", RecurrenceRule.Weekly().ToDisplayText());
        Assert.Equal("每月", RecurrenceRule.Monthly().ToDisplayText());
    }
}
