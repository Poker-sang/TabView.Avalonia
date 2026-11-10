using System;
using System.Linq;

namespace TabView.Avalonia.Primitives;

/// <summary>Arranges tab content, a drag thumb and a close button using the same compact rules in every theme.</summary>
public sealed class TabHeaderPanel : Panel
{
    /// <summary>The default width at which inactive tabs hide their close buttons, in DIPs.</summary>
    public const double DefaultCompactWidthThreshold = 75;

    /// <summary>Defines <see cref="AllowCloseButton"/>.</summary>
    public static readonly StyledProperty<bool> AllowCloseButtonProperty =
        AvaloniaProperty.Register<TabHeaderPanel, bool>(nameof(AllowCloseButton), true);
    /// <summary>Defines <see cref="IsActive"/>.</summary>
    public static readonly StyledProperty<bool> IsActiveProperty =
        AvaloniaProperty.Register<TabHeaderPanel, bool>(nameof(IsActive));
    /// <summary>Defines <see cref="TabWidth"/>.</summary>
    public static readonly StyledProperty<double> TabWidthProperty =
        AvaloniaProperty.Register<TabHeaderPanel, double>(nameof(TabWidth));
    /// <summary>Defines <see cref="CompactWidthThreshold"/>.</summary>
    public static readonly StyledProperty<double> CompactWidthThresholdProperty =
        AvaloniaProperty.Register<TabHeaderPanel, double>(nameof(CompactWidthThreshold), DefaultCompactWidthThreshold);
    /// <summary>Defines <see cref="Padding"/>.</summary>
    public static readonly StyledProperty<Thickness> PaddingProperty = Border.PaddingProperty.AddOwner<TabHeaderPanel>();
    /// <summary>Defines <see cref="MinimumCloseButtonWidth"/>.</summary>
    public static readonly StyledProperty<double> MinimumCloseButtonWidthProperty =
        AvaloniaProperty.Register<TabHeaderPanel, double>(nameof(MinimumCloseButtonWidth));

    static TabHeaderPanel() => AffectsMeasure<TabHeaderPanel>(AllowCloseButtonProperty, IsActiveProperty,
        TabWidthProperty, CompactWidthThresholdProperty, PaddingProperty, MinimumCloseButtonWidthProperty);

    /// <summary>Creates a panel that clips foreground content independently of the tab's outer outline.</summary>
    public TabHeaderPanel() => ClipToBounds = true;

    /// <summary>Whether the tab's close setting and fixed-tab policy permit closing.</summary>
    public bool AllowCloseButton { get => GetValue(AllowCloseButtonProperty); set => SetValue(AllowCloseButtonProperty, value); }
    /// <summary>Whether this is the selected tab, whose close button takes priority over content.</summary>
    public bool IsActive { get => GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }
    /// <summary>The full tab width before padding, used for the compact threshold.</summary>
    public double TabWidth { get => GetValue(TabWidthProperty); set => SetValue(TabWidthProperty, value); }
    /// <summary>The maximum width at which inactive close buttons are hidden.</summary>
    public double CompactWidthThreshold { get => GetValue(CompactWidthThresholdProperty); set => SetValue(CompactWidthThresholdProperty, value); }
    /// <summary>Content padding. The close target takes priority over horizontal padding in very narrow tabs.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    /// <summary>The minimum close target width that can display the theme's whole glyph.</summary>
    public double MinimumCloseButtonWidth { get => GetValue(MinimumCloseButtonWidthProperty); set => SetValue(MinimumCloseButtonWidthProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = Part("PART_HeaderContent");
        var close = Part("PART_CloseButton");
        if (content is null || close is null)
            return default;

        content.SetCurrentValue(ClipToBoundsProperty, true);
        MeasureClose(close, availableSize);
        var rightPadding = Math.Min(Padding.Right, Math.Max(0, availableSize.Width - close.DesiredSize.Width));
        var contentWidth = Math.Max(0, availableSize.Width - Padding.Left - rightPadding - close.DesiredSize.Width);
        content.Measure(new Size(contentWidth, Math.Max(0, availableSize.Height - Padding.Top - Padding.Bottom)));
        Part("PART_Thumb")?.Measure(availableSize);
        return new Size(Math.Min(availableSize.Width, content.DesiredSize.Width + Padding.Left + rightPadding + close.DesiredSize.Width),
            Math.Max(content.DesiredSize.Height, close.DesiredSize.Height) + Padding.Top + Padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = Part("PART_HeaderContent");
        var close = Part("PART_CloseButton");
        if (content is null || close is null)
            return finalSize;

        MeasureClose(close, finalSize);
        var closeWidth = Math.Min(finalSize.Width, close.DesiredSize.Width);
        var rightPadding = Math.Min(Padding.Right, Math.Max(0, finalSize.Width - closeWidth));
        var closeLeft = finalSize.Width - rightPadding - closeWidth;
        content.Arrange(new Rect(Padding.Left, Padding.Top, Math.Max(0, closeLeft - Padding.Left),
            Math.Max(0, finalSize.Height - Padding.Top - Padding.Bottom)));
        Part("PART_Thumb")?.Arrange(new Rect(finalSize));
        close.Arrange(new Rect(closeLeft, 0, closeWidth, finalSize.Height));
        return finalSize;
    }

    private void MeasureClose(Control close, Size availableSize)
    {
        var availableWidth = Math.Max(0, availableSize.Width - close.Margin.Left - close.Margin.Right);
        close.IsVisible = AllowCloseButton && (IsActive || TabWidth > CompactWidthThreshold) &&
            availableWidth >= MinimumCloseButtonWidth;
        // Preserve the themed Width; only constrain the target when the entire tab is narrower.
        close.SetCurrentValue(MaxWidthProperty, availableWidth);
        close.Measure(availableSize);
    }

    private Control? Part(string name) => Children.FirstOrDefault(child => child.Name == name);
}
