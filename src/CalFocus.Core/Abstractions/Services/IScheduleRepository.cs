using CalFocus.Core.Domain.Entities;

namespace CalFocus.Core.Abstractions.Services;

public interface IScheduleRepository
{
    Task<ScheduleItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleItem>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleItem>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    Task<Guid> AddAsync(ScheduleItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScheduleItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
