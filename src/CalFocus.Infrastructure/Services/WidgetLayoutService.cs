using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;
using CalFocus.Infrastructure.Persistence.Json;

namespace CalFocus.Infrastructure.Services;

public sealed class WidgetLayoutService : IWidgetLayoutService
{
    private readonly IAppDataPathService _pathService;
    private readonly JsonFileStore _jsonFileStore;

    public WidgetLayoutService(IAppDataPathService pathService, JsonFileStore jsonFileStore)
    {
        _pathService = pathService;
        _jsonFileStore = jsonFileStore;
    }

    public async Task<IReadOnlyList<WidgetInstance>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var data = await _jsonFileStore.ReadAsync<List<WidgetInstance>>(_pathService.WidgetLayoutPath, cancellationToken);
        return data ?? new List<WidgetInstance>();
    }

    public async Task SaveAllAsync(IReadOnlyList<WidgetInstance> widgets, CancellationToken cancellationToken = default)
    {
        await _jsonFileStore.WriteAsync(_pathService.WidgetLayoutPath, widgets, cancellationToken);
    }
}
