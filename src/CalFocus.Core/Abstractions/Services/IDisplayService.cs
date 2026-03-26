using CalFocus.Core.Domain.Entities;

namespace CalFocus.Core.Abstractions.Services;

public interface IDisplayService
{
    IReadOnlyList<DisplayProfile> GetDisplays();
    DisplayProfile? GetPrimaryDisplay();
    DisplayProfile? GetDisplayForPoint(double x, double y);
}
