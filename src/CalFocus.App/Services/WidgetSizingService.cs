using CalFocus.Core.Domain.Entities;

namespace CalFocus.App.Services;

public static class WidgetSizingService
{
    private readonly record struct WidgetSizingRule(
        double AspectRatio,
        double SmallWidthPercent,
        double MediumWidthPercent,
        double LargeWidthPercent,
        double MinWidthPercent,
        double MaxWidthPercent,
        double MaxHeightPercent);

    public static string NormalizeSizeMode(string? sizeMode)
    {
        if (string.Equals(sizeMode, WidgetInstance.SizeModeSmall, StringComparison.OrdinalIgnoreCase))
        {
            return WidgetInstance.SizeModeSmall;
        }

        if (string.Equals(sizeMode, WidgetInstance.SizeModeLarge, StringComparison.OrdinalIgnoreCase))
        {
            return WidgetInstance.SizeModeLarge;
        }

        if (string.Equals(sizeMode, WidgetInstance.SizeModeFree, StringComparison.OrdinalIgnoreCase))
        {
            return WidgetInstance.SizeModeFree;
        }

        return WidgetInstance.SizeModeMedium;
    }

    public static bool IsPresetMode(string? sizeMode)
    {
        return !string.Equals(NormalizeSizeMode(sizeMode), WidgetInstance.SizeModeFree, StringComparison.Ordinal);
    }

    public static (int Width, int Height) CalculatePresetSize(string widgetType, string? sizeMode, int workAreaWidth, int workAreaHeight)
    {
        var mode = NormalizeSizeMode(sizeMode);
        var rule = ResolveRule(widgetType);
        var safeAreaWidth = Math.Max(1, workAreaWidth);
        var safeAreaHeight = Math.Max(1, workAreaHeight);

        var widthPercent = mode switch
        {
            WidgetInstance.SizeModeSmall => rule.SmallWidthPercent,
            WidgetInstance.SizeModeLarge => rule.LargeWidthPercent,
            _ => rule.MediumWidthPercent
        };

        var requestedWidth = Math.Max(1, (int)Math.Round(safeAreaWidth * widthPercent));
        var requestedHeight = Math.Max(1, (int)Math.Round(requestedWidth / rule.AspectRatio));

        return ConstrainByRule(rule, requestedWidth, requestedHeight, safeAreaWidth, safeAreaHeight, preferHeightSafety: true);
    }

    public static (int Width, int Height) ConstrainSize(
        string widgetType,
        int requestedWidth,
        int requestedHeight,
        int workAreaWidth,
        int workAreaHeight,
        bool preferHeightSafety)
    {
        var safeAreaWidth = Math.Max(1, workAreaWidth);
        var safeAreaHeight = Math.Max(1, workAreaHeight);

        var rule = ResolveRule(widgetType);
        return ConstrainByRule(
            rule,
            Math.Max(1, requestedWidth),
            Math.Max(1, requestedHeight),
            safeAreaWidth,
            safeAreaHeight,
            preferHeightSafety);
    }

    private static (int Width, int Height) ConstrainByRule(
        WidgetSizingRule rule,
        int requestedWidth,
        int requestedHeight,
        int workAreaWidth,
        int workAreaHeight,
        bool preferHeightSafety)
    {
        var maxWidth = Math.Clamp((int)Math.Round(workAreaWidth * rule.MaxWidthPercent), 1, workAreaWidth);
        var maxHeight = Math.Clamp((int)Math.Round(workAreaHeight * rule.MaxHeightPercent), 1, workAreaHeight);

        var width = Math.Max(1, requestedWidth);
        var height = Math.Max(1, requestedHeight);

        // 先按当前拖拽趋势保持比例，再执行边界约束。
        var heightByWidth = Math.Max(1, (int)Math.Round(width / rule.AspectRatio));
        var widthByHeight = Math.Max(1, (int)Math.Round(height * rule.AspectRatio));

        if (Math.Abs(height - heightByWidth) <= Math.Abs(width - widthByHeight))
        {
            height = heightByWidth;
        }
        else
        {
            width = widthByHeight;
        }

        if (preferHeightSafety)
        {
            if (height > maxHeight)
            {
                height = maxHeight;
                width = Math.Max(1, (int)Math.Round(height * rule.AspectRatio));
            }

            if (width > maxWidth)
            {
                width = maxWidth;
                height = Math.Max(1, (int)Math.Round(width / rule.AspectRatio));
            }
        }
        else
        {
            if (width > maxWidth)
            {
                width = maxWidth;
                height = Math.Max(1, (int)Math.Round(width / rule.AspectRatio));
            }

            if (height > maxHeight)
            {
                height = maxHeight;
                width = Math.Max(1, (int)Math.Round(height * rule.AspectRatio));
            }
        }

        var minWidth = Math.Clamp((int)Math.Round(workAreaWidth * rule.MinWidthPercent), 1, maxWidth);
        var minHeight = Math.Clamp((int)Math.Round(minWidth / rule.AspectRatio), 1, maxHeight);

        if (width < minWidth)
        {
            width = minWidth;
            height = Math.Max(1, (int)Math.Round(width / rule.AspectRatio));
        }

        if (height < minHeight)
        {
            height = minHeight;
            width = Math.Max(1, (int)Math.Round(height * rule.AspectRatio));
        }

        if (height > maxHeight)
        {
            height = maxHeight;
            width = Math.Max(1, (int)Math.Round(height * rule.AspectRatio));
        }

        if (width > maxWidth)
        {
            width = maxWidth;
            height = Math.Max(1, (int)Math.Round(width / rule.AspectRatio));
        }

        width = Math.Clamp(width, 1, workAreaWidth);
        height = Math.Clamp(height, 1, workAreaHeight);
        return (width, height);
    }

    private static WidgetSizingRule ResolveRule(string widgetType)
    {
        if (string.Equals(widgetType, "Schedule", StringComparison.OrdinalIgnoreCase))
        {
            return new WidgetSizingRule(
                AspectRatio: 8d / 5d,
                SmallWidthPercent: 0.17,
                MediumWidthPercent: 0.22,
                LargeWidthPercent: 0.28,
                MinWidthPercent: 0.13,
                MaxWidthPercent: 0.42,
                MaxHeightPercent: 0.56);
        }

        if (string.Equals(widgetType, "Pomodoro", StringComparison.OrdinalIgnoreCase))
        {
            return new WidgetSizingRule(
                AspectRatio: 4d / 3d,
                SmallWidthPercent: 0.15,
                MediumWidthPercent: 0.19,
                LargeWidthPercent: 0.24,
                MinWidthPercent: 0.12,
                MaxWidthPercent: 0.36,
                MaxHeightPercent: 0.54);
        }

        return new WidgetSizingRule(
            AspectRatio: 16d / 9d,
            SmallWidthPercent: 0.14,
            MediumWidthPercent: 0.18,
            LargeWidthPercent: 0.22,
            MinWidthPercent: 0.11,
            MaxWidthPercent: 0.32,
            MaxHeightPercent: 0.42);
    }
}
