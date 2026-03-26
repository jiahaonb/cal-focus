using CalFocus.Core.Domain.Enums;

namespace CalFocus.Core.Domain.Entities;

public sealed class PlanItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public PlanPeriodType PeriodType { get; set; } = PlanPeriodType.Week;
    public PlanStatus Status { get; set; } = PlanStatus.NotStarted;
    public List<Guid> RelatedScheduleIds { get; set; } = new();
}
