using System.Diagnostics;

namespace TabView.Avalonia;

[DebuggerDisplay($"{nameof(TabsViewItem)}({{Header}})")]
public class TabsViewItem : TabItem
{
    #region Private Fields

    private int _prevZindex;

    #endregion
    
    #region Avalonia Properties

    public static readonly StyledProperty<double> XProperty =
        AvaloniaProperty.Register<TabsViewItem, double>(nameof(X));

    public static readonly StyledProperty<double> YProperty =
        AvaloniaProperty.Register<TabsViewItem, double>(nameof(Y));

    public static readonly DirectProperty<TabsViewItem, bool> IsDraggingProperty =
        AvaloniaProperty.RegisterDirect<TabsViewItem, bool>(nameof(IsDragging),
            o => o.IsDragging, (o, v) => o.IsDragging = v);
    
    public static readonly DirectProperty<TabsViewItem, int> LogicalIndexProperty =
        AvaloniaProperty.RegisterDirect<TabsViewItem, int>(nameof(LogicalIndex),
            o => o.LogicalIndex, (o, v) => o.LogicalIndex = v);

    public static readonly DirectProperty<TabsViewItem, bool> IsSiblingDraggingProperty =
        AvaloniaProperty.RegisterDirect<TabsViewItem, bool>(nameof(IsSiblingDragging),
            o => o.IsSiblingDragging, (o, v) => o.IsSiblingDragging = v);

    #endregion

    #region Public Properties

    public double X
    {
        get => GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public double Y
    {
        get => GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public int LogicalIndex
    {
        get;
        internal set => SetAndRaise(LogicalIndexProperty, ref field, value);
    }

    public bool IsDragging
    {
        get;
        internal set => SetAndRaise(IsDraggingProperty, ref field, value);
    }

    public bool IsSiblingDragging
    {
        get;
        internal set => SetAndRaise(IsSiblingDraggingProperty, ref field, value);
    }

    #endregion

    #region Routed Events

    public static readonly RoutedEvent<RoutedEventArgs> MiddleClick =
        RoutedEvent.Register<TabsViewItem, RoutedEventArgs>(nameof(MiddleClick), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<DragTabDragStartedEventArgs> DragStarted =
        RoutedEvent.Register<TabsViewItem, DragTabDragStartedEventArgs>(nameof(DragStarted), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<DragTabDragDeltaEventArgs> DragDelta =
        RoutedEvent.Register<TabsViewItem, DragTabDragDeltaEventArgs>(nameof(DragDelta), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<DragTabDragCompletedEventArgs> DragCompleted =
        RoutedEvent.Register<TabsViewItem, DragTabDragCompletedEventArgs>(nameof(DragCompleted), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<DragTabDragDeltaEventArgs> PreviewDragDelta =
        RoutedEvent.Register<TabsViewItem, DragTabDragDeltaEventArgs>(nameof(PreviewDragDelta), RoutingStrategies.Tunnel);
    
    #endregion

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<LeftPressedThumb>("PART_Thumb") is not { } thumb)
            return;

        thumb.PointerReleased += ThumbOnPointerReleased;
        thumb.DragStarted += ThumbOnDragStarted;
        thumb.DragDelta += ThumbOnDragDelta;
        thumb.DragCompleted += ThumbOnDragCompleted;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);

        if (IsSelected || IsDragging)
            return;
        
        _prevZindex = ZIndex;
        
        ZIndex = ZIndexes.PointerOver;
    }
    
    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);

        if (!(IsSelected || IsDragging))
            ZIndex = _prevZindex;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty)
            if (change.GetNewValue<bool>())
                ZIndex = ZIndexes.Selected;
    }

    public override string ToString()
    {
        return $"{nameof(TabsViewItem)}.{nameof(Header)}:{Header}";
    }

    internal void ClearReusableContent()
    {
        Icon = null;
        IconTemplate = null;
        Header = null;
        HeaderTemplate = null;
        Content = null;
        ContentTemplate = null;
    }

    private void ThumbOnPointerReleased(object? sender, PointerReleasedEventArgs args)
    {
        if (args.InitialPressMouseButton is MouseButton.Middle)
            RaiseEvent(new RoutedEventArgs(MiddleClick, this));
    }

    private void ThumbOnDragStarted(object? sender, VectorEventArgs args)
    {
        RaiseEvent(new DragTabDragStartedEventArgs(DragStarted, this, args));
    }
    
    private void ThumbOnDragDelta(object? sender, VectorEventArgs e)
    {
        if (sender is not LeftPressedThumb thumb)
            return;
        var screenPosition = thumb.LastScreenPosition;

        var previewEventArgs = new DragTabDragDeltaEventArgs(PreviewDragDelta, this, e)
        {
            PointerScreenPosition = screenPosition
        };
        RaiseEvent(previewEventArgs);
        // if (previewEventArgs.Cancel)
        //     thumb.CancelDrag();
        if (!previewEventArgs.Handled)
        {
            var eventArgs = new DragTabDragDeltaEventArgs(DragDelta, this, e)
            {
                PointerScreenPosition = screenPosition
            };
            RaiseEvent(eventArgs);
            //if (eventArgs.Cancel)
            //    thumb.CancelDrag();
        }
    }

    private void ThumbOnDragCompleted(object? sender, VectorEventArgs e)
    {
        var args = new DragTabDragCompletedEventArgs(DragCompleted, this, e);
        RaiseEvent(args);
    }
}
