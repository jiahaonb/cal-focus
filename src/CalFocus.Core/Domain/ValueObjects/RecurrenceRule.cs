namespace CalFocus.Core.Domain.ValueObjects;

/// <summary>
/// 重复规则值对象，支持简化的重复规则（无/每天/每周/每月）
/// </summary>
public sealed class RecurrenceRule
{
    public RecurrenceFrequency Frequency { get; }
    public int? Interval { get; }
    public DateOnly? EndDate { get; }
    public int? MaxOccurrences { get; }

    private RecurrenceRule(RecurrenceFrequency frequency, int? interval = null, DateOnly? endDate = null, int? maxOccurrences = null)
    {
        Frequency = frequency;
        Interval = interval ?? 1;
        EndDate = endDate;
        MaxOccurrences = maxOccurrences;
    }

    /// <summary>
    /// 创建无重复规则
    /// </summary>
    public static RecurrenceRule None() => new(RecurrenceFrequency.None);

    /// <summary>
    /// 创建每天重复规则
    /// </summary>
    public static RecurrenceRule Daily(int interval = 1, DateOnly? endDate = null, int? maxOccurrences = null)
        => new(RecurrenceFrequency.Daily, interval, endDate, maxOccurrences);

    /// <summary>
    /// 创建每周重复规则
    /// </summary>
    public static RecurrenceRule Weekly(int interval = 1, DateOnly? endDate = null, int? maxOccurrences = null)
        => new(RecurrenceFrequency.Weekly, interval, endDate, maxOccurrences);

    /// <summary>
    /// 创建每月重复规则
    /// </summary>
    public static RecurrenceRule Monthly(int interval = 1, DateOnly? endDate = null, int? maxOccurrences = null)
        => new(RecurrenceFrequency.Monthly, interval, endDate, maxOccurrences);

    /// <summary>
    /// 从字符串解析规则（支持"无"/"每天"/"每周"/"每月"）
    /// </summary>
    public static RecurrenceRule Parse(string? ruleText)
    {
        if (string.IsNullOrWhiteSpace(ruleText))
            return None();

        return ruleText.Trim() switch
        {
            "无" => None(),
            "每天" => Daily(),
            "每周" => Weekly(),
            "每月" => Monthly(),
            _ => None()
        };
    }

    /// <summary>
    /// 转换为显示文本
    /// </summary>
    public string ToDisplayText() => Frequency switch
    {
        RecurrenceFrequency.None => "无",
        RecurrenceFrequency.Daily => "每天",
        RecurrenceFrequency.Weekly => "每周",
        RecurrenceFrequency.Monthly => "每月",
        _ => "无"
    };

    /// <summary>
    /// 生成指定日期范围内的所有实例日期
    /// </summary>
    public IEnumerable<DateOnly> GenerateOccurrences(DateOnly startDate, DateOnly rangeStart, DateOnly rangeEnd)
    {
        if (Frequency == RecurrenceFrequency.None)
        {
            if (startDate >= rangeStart && startDate <= rangeEnd)
                yield return startDate;
            yield break;
        }

        var current = startDate;
        var occurrenceCount = 0;

        while (current <= rangeEnd)
        {
            if (current >= rangeStart)
            {
                yield return current;
                occurrenceCount++;

                if (MaxOccurrences.HasValue && occurrenceCount >= MaxOccurrences.Value)
                    yield break;
            }

            if (EndDate.HasValue && current >= EndDate.Value)
                yield break;

            current = Frequency switch
            {
                RecurrenceFrequency.Daily => current.AddDays(Interval ?? 1),
                RecurrenceFrequency.Weekly => current.AddDays((Interval ?? 1) * 7),
                RecurrenceFrequency.Monthly => AddMonths(current, Interval ?? 1),
                _ => current.AddDays(1)
            };
        }
    }

    private static DateOnly AddMonths(DateOnly date, int months)
    {
        var newMonth = date.Month + months;
        var newYear = date.Year;

        while (newMonth > 12)
        {
            newMonth -= 12;
            newYear++;
        }

        var maxDay = DateTime.DaysInMonth(newYear, newMonth);
        var newDay = Math.Min(date.Day, maxDay);

        return new DateOnly(newYear, newMonth, newDay);
    }

    public override string ToString() => ToDisplayText();
}

public enum RecurrenceFrequency
{
    None = 0,
    Daily = 1,
    Weekly = 2,
    Monthly = 3
}
