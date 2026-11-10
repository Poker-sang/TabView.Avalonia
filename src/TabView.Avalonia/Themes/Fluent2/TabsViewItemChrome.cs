using System;
using Avalonia.Media;

namespace TabView.Avalonia;

/// <summary>
/// The continuous selected-tab outline from WinUI's TabViewItem.UpdateTabGeometry:
/// rounded top corners and two concave 4px feet. Rendering one geometry avoids seams
/// between separately antialiased borders and paths, including at fractional DPI.
/// </summary>
internal sealed class TabsViewItemChrome : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<TabsViewItemChrome>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<TabsViewItemChrome>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<TabsViewItemChrome>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<TabsViewItemChrome>();

    public static readonly StyledProperty<double> FootRadiusProperty =
        AvaloniaProperty.Register<TabsViewItemChrome, double>(nameof(FootRadius), 4);

    public static readonly StyledProperty<double> ContentOverlapProperty =
        AvaloniaProperty.Register<TabsViewItemChrome, double>(nameof(ContentOverlap), 1);

    static TabsViewItemChrome()
    {
        AffectsRender<TabsViewItemChrome>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty,
            CornerRadiusProperty, FootRadiusProperty, ContentOverlapProperty);
    }

    public TabsViewItemChrome() => UpdateMargin();

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public double FootRadius
    {
        get => GetValue(FootRadiusProperty);
        set => SetValue(FootRadiusProperty, value);
    }

    public double ContentOverlap
    {
        get => GetValue(ContentOverlapProperty);
        set => SetValue(ContentOverlapProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FootRadiusProperty || change.Property == ContentOverlapProperty)
            UpdateMargin();
    }

    private void UpdateMargin() => Margin = new Thickness(-FootRadius, 0, -FootRadius, -ContentOverlap);

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        var join = Math.Clamp(height - ContentOverlap, 0, height);
        var foot = Math.Clamp(FootRadius, 0, Math.Min(width / 2, join));
        var stroke = Math.Max(0, BorderThickness.Top);
        var inset = stroke / 2;
        var maxRadius = Math.Max(0, Math.Min((width - 2 * foot) / 2, join - foot));
        var left = Math.Max(0, Math.Clamp(CornerRadius.TopLeft, 0, maxRadius) - inset);
        var right = Math.Max(0, Math.Clamp(CornerRadius.TopRight, 0, maxRadius) - inset);
        var arcSize = new Size(foot + inset, foot + inset);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            // Keep the feet at the content join; the overlap only seals the fill below it.
            path.BeginFigure(new Point(0, join + inset), true);
            path.ArcTo(new Point(foot + inset, join - foot), arcSize, 0, false, SweepDirection.CounterClockwise);
            path.LineTo(new Point(foot + inset, inset + left));
            path.ArcTo(new Point(foot + inset + left, inset), new Size(left, left), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot - inset - right, inset));
            path.ArcTo(new Point(width - foot - inset, inset + right), new Size(right, right), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot - inset, join - foot));
            path.ArcTo(new Point(width, join + inset), arcSize, 0, false, SweepDirection.CounterClockwise);
            // Fill closes implicitly. Leaving the stroke open avoids a content divider.
            path.EndFigure(false);
        }
        context.DrawGeometry(Background, stroke > 0 ? new Pen(BorderBrush, stroke) : null, geometry);
    }
}
