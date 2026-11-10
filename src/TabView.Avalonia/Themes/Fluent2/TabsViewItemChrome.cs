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

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<TabsViewItemChrome>();

    public static readonly StyledProperty<double> FootRadiusProperty =
        AvaloniaProperty.Register<TabsViewItemChrome, double>(nameof(FootRadius), 4);

    static TabsViewItemChrome()
    {
        AffectsRender<TabsViewItemChrome>(BackgroundProperty, CornerRadiusProperty, FootRadiusProperty);
    }

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

    public double FootRadius
    {
        get => GetValue(FootRadiusProperty);
        set => SetValue(FootRadiusProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        var foot = Math.Clamp(FootRadius, 0, Math.Min(width / 2, height));
        var maxRadius = Math.Min((width - 2 * foot) / 2, height - foot);
        var left = Math.Clamp(CornerRadius.TopLeft, 0, maxRadius);
        var right = Math.Clamp(CornerRadius.TopRight, 0, maxRadius);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(new Point(0, height), true);
            path.ArcTo(new Point(foot, height - foot), new Size(foot, foot), 0, false, SweepDirection.CounterClockwise);
            path.LineTo(new Point(foot, left));
            path.ArcTo(new Point(foot + left, 0), new Size(left, left), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot - right, 0));
            path.ArcTo(new Point(width - foot, right), new Size(right, right), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot, height - foot));
            path.ArcTo(new Point(width, height), new Size(foot, foot), 0, false, SweepDirection.CounterClockwise);
            path.EndFigure(true);
        }
        context.DrawGeometry(Background, null, geometry);
    }
}
