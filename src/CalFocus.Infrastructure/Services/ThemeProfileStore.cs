using CalFocus.Core.Abstractions.Services;
using CalFocus.Core.Domain.Entities;
using CalFocus.Infrastructure.Persistence.Json;

namespace CalFocus.Infrastructure.Services;

public sealed class ThemeProfileStore : IThemeProfileStore
{
    private readonly IAppDataPathService _pathService;
    private readonly JsonFileStore _jsonFileStore;

    public ThemeProfileStore(IAppDataPathService pathService, JsonFileStore jsonFileStore)
    {
        _pathService = pathService;
        _jsonFileStore = jsonFileStore;
    }

    public async Task<ThemeProfile> LoadAsync(CancellationToken cancellationToken = default)
    {
        var profile = await _jsonFileStore.ReadAsync<ThemeProfile>(_pathService.ThemeProfilePath, cancellationToken);
        return profile ?? new ThemeProfile();
    }

    public Task SaveAsync(ThemeProfile profile, CancellationToken cancellationToken = default)
    {
        return _jsonFileStore.WriteAsync(_pathService.ThemeProfilePath, profile, cancellationToken);
    }
}
