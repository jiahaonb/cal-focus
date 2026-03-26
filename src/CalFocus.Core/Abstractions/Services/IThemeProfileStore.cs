using CalFocus.Core.Domain.Entities;

namespace CalFocus.Core.Abstractions.Services;

public interface IThemeProfileStore
{
    Task<ThemeProfile> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(ThemeProfile profile, CancellationToken cancellationToken = default);
}
