using CalFocus.Core.Domain.Entities;

namespace CalFocus.Core.Abstractions.Services;

public interface IWidgetLayoutService
{
    Task<IReadOnlyList<WidgetInstance>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SaveAllAsync(IReadOnlyList<WidgetInstance> widgets, CancellationToken cancellationToken = default);
}
