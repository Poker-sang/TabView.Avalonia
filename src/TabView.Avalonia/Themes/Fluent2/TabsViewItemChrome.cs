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

    static TabsViewItemChrome()
    {
        AffectsRender<TabsViewItemChrome>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty,
            CornerRadiusProperty, FootRadiusProperty);
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FootRadiusProperty)
            UpdateMargin();
    }

    private void UpdateMargin() => Margin = new Thickness(-FootRadius, 0, -FootRadius, 0);

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
            return;

        var stroke = Math.Max(0, BorderThickness.Top);
        var inset = stroke / 2;
        var foot = Math.Clamp(FootRadius + inset, 0, Math.Min(width / 2, height));
        var maxRadius = Math.Max(0, Math.Min((width - 2 * foot) / 2, height - foot - inset));
        var left = Math.Clamp(CornerRadius.TopLeft - inset, 0, maxRadius);
        var right = Math.Clamp(CornerRadius.TopRight - inset, 0, maxRadius);
        var arcSize = new Size(foot, foot);
        var geometry = new StreamGeometry();
        using (var path = geometry.Open())
        {
            path.BeginFigure(new Point(0, height), true);
            path.ArcTo(new Point(foot, height - foot), arcSize, 0, false, SweepDirection.CounterClockwise);
            path.LineTo(new Point(foot, inset + left));
            path.ArcTo(new Point(foot + left, inset), new Size(left, left), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot - right, inset));
            path.ArcTo(new Point(width - foot, inset + right), new Size(right, right), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - foot, height - foot));
            path.ArcTo(new Point(width, height), arcSize, 0, false, SweepDirection.CounterClockwise);
            // Fill closes implicitly. Leaving the stroke open avoids a content divider.
            path.EndFigure(false);
        }
        // Clip the stroke at the join so it never overdraws a translucent content layer.
        using (context.PushClip(new Rect(Bounds.Size)))
            context.DrawGeometry(Background, stroke > 0 ? new Pen(BorderBrush, stroke) : null, geometry);
    }
}
