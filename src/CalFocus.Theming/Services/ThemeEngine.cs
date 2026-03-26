using CalFocus.Core.Domain.Entities;

namespace CalFocus.Theming.Services;

public sealed class ThemeEngine
{
    public ThemeProfile Current { get; } = new();
}
