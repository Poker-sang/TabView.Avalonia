using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace TabView.Avalonia;

/// <summary>Shares one circular-arc outline between the tab background and its Material state layer.</summary>
internal sealed class Material3TabBackground : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<Material3TabBackground>();
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<Material3TabBackground>();
    public static readonly StyledProperty<bool> IsConnectedProperty = AvaloniaProperty.Register<Material3TabBackground, bool>(nameof(IsConnected));
    public static readonly StyledProperty<Thickness> BodyInsetProperty = AvaloniaProperty.Register<Material3TabBackground, Thickness>(nameof(BodyInset));
    public static readonly StyledProperty<double> FootRadiusProperty = AvaloniaProperty.Register<Material3TabBackground, double>(nameof(FootRadius));
    public static readonly StyledProperty<double> ContentOverlapProperty = AvaloniaProperty.Register<Material3TabBackground, double>(nameof(ContentOverlap));

    static Material3TabBackground() => AffectsRender<Material3TabBackground>(BackgroundProperty, CornerRadiusProperty, IsConnectedProperty, FootRadiusProperty);
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public bool IsConnected { get => GetValue(IsConnectedProperty); set => SetValue(IsConnectedProperty, value); }
    public Thickness BodyInset { get => GetValue(BodyInsetProperty); set => SetValue(BodyInsetProperty, value); }
    public double FootRadius { get => GetValue(FootRadiusProperty); set => SetValue(FootRadiusProperty, value); }
    public double ContentOverlap { get => GetValue(ContentOverlapProperty); set => SetValue(ContentOverlapProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsConnectedProperty || change.Property == BodyInsetProperty ||
            change.Property == FootRadiusProperty || change.Property == ContentOverlapProperty)
            Margin = IsConnected
                ? new Thickness(BodyInset.Left - FootRadius, BodyInset.Top, BodyInset.Right - FootRadius, -ContentOverlap)
                : BodyInset;
        if (change.Property == IsConnectedProperty || change.Property == CornerRadiusProperty || change.Property == FootRadiusProperty)
            InvalidateArrange();
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        Clip = CreateOutline(finalSize);
        return result;
    }

    public override void Render(DrawingContext context) => context.DrawGeometry(Background, null, Clip);

    private Geometry CreateOutline(Size size)
    {
        var width = Math.Max(0, size.Width);
        var height = Math.Max(0, size.Height);
        var geometry = new StreamGeometry();
        using var path = geometry.Open();
        if (!IsConnected)
        {
            // Clamp one scalar radius per corner. Independently fitting X/Y radii at narrow
            // widths would turn circles into ellipses, as happens with a scaled round rectangle.
            var maximum = Math.Min(width, height) / 2;
            var tl = Math.Clamp(CornerRadius.TopLeft, 0, maximum);
            var tr = Math.Clamp(CornerRadius.TopRight, 0, maximum);
            var br = Math.Clamp(CornerRadius.BottomRight, 0, maximum);
            var bl = Math.Clamp(CornerRadius.BottomLeft, 0, maximum);
            path.BeginFigure(new Point(0, tl), true);
            path.ArcTo(new Point(tl, 0), new Size(tl, tl), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width - tr, 0));
            path.ArcTo(new Point(width, tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(width, height - br));
            path.ArcTo(new Point(width - br, height), new Size(br, br), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(bl, height));
            path.ArcTo(new Point(0, height - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise);
            path.EndFigure(true);
            return geometry;
        }
        var foot = Math.Clamp(FootRadius, 0, Math.Min(width / 2, height));
        var maxRadius = Math.Max(0, Math.Min((width - 2 * foot) / 2, height - foot));
        var left = Math.Clamp(CornerRadius.TopLeft, 0, maxRadius);
        var right = Math.Clamp(CornerRadius.TopRight, 0, maxRadius);
        path.BeginFigure(new Point(0, height), true);
        path.ArcTo(new Point(foot, height - foot), new Size(foot, foot), 0, false, SweepDirection.CounterClockwise);
        path.LineTo(new Point(foot, left));
        path.ArcTo(new Point(foot + left, 0), new Size(left, left), 0, false, SweepDirection.Clockwise);
        path.LineTo(new Point(width - foot - right, 0));
        path.ArcTo(new Point(width - foot, right), new Size(right, right), 0, false, SweepDirection.Clockwise);
        path.LineTo(new Point(width - foot, height - foot));
        path.ArcTo(new Point(width, height), new Size(foot, foot), 0, false, SweepDirection.CounterClockwise);
        path.EndFigure(true);
        return geometry;
    }
}
