using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Shapes;
using Avalonia.VisualTree;
using TabView.Avalonia.Primitives;

namespace TabView.Avalonia;

public class TabsView : TabbedPage
{
    #region Constants

    private const double DefaultTabWidth = 140;

    public const double WindowsAndLinuxDefaultLeftThumbWidth = 4d;
    public const double MacOsDefaultLeftThumbWidth = 80d;

    public const double WindowsDefaultRightThumbWidth = 160d;
    public const double MacOsDefaultRightThumbWidth = 50d;

    #endregion

    #region Private Fields

    private readonly TabsPanel _tabsPanel;
    private TabsViewItem? _draggedItem;
    private bool _dragging;
    private DragTabControl? _tabControl;
    private Control? _leftHeaderContentDraggable;
    private Control? _rightHeaderContentDraggable;

    // Inter-tab state: active during a tear-off / floating window drag
    private FloatingDragState? _floatingState;

    // Pointer position relative to DragTabItem at drag start (DIPs).
    // Captured once so that tear-off preserves the original grab point.
    private Point _dragStartPointerInTab;

    #endregion

    #region Static Instance Tracking

    private static readonly HashSet<TabsView> _LoadedInstances = [];

    /// <summary>
    /// All currently loaded (attached to visual tree) <see cref="TabsView"/> instances.
    /// Used for finding merge targets during inter-tab drag.
    /// </summary>
    internal static IReadOnlySet<TabsView> LoadedInstances => _LoadedInstances;

    #endregion

    #region Avalonia Properties

    public static readonly StyledProperty<double> AdjacentHeaderItemOffsetProperty =
        AvaloniaProperty.Register<TabsView, double>(nameof(AdjacentHeaderItemOffset), defaultValue: 0);

    public static readonly StyledProperty<double> TabItemWidthProperty =
        AvaloniaProperty.Register<TabsView, double>(nameof(TabItemWidth), defaultValue: DefaultTabWidth);

    public static readonly StyledProperty<bool> ShowDefaultCloseButtonProperty =
        AvaloniaProperty.Register<TabsView, bool>(nameof(ShowDefaultCloseButton), defaultValue: true);

    public static readonly StyledProperty<bool> ShowDefaultAddButtonProperty =
        AvaloniaProperty.Register<TabsView, bool>(nameof(ShowDefaultAddButton), defaultValue: true);

    public static readonly StyledProperty<int> FixedHeaderCountProperty =
        AvaloniaProperty.Register<TabsView, int>(nameof(FixedHeaderCount), defaultValue: 0);

    public static readonly StyledProperty<double> LeftIndentProperty =
        AvaloniaProperty.Register<TabsView, double>(nameof(LeftIndent),
            defaultValue: OperatingSystem.IsMacOS() ? MacOsDefaultLeftThumbWidth : WindowsAndLinuxDefaultLeftThumbWidth);

    public static readonly StyledProperty<double> RightIndentProperty =
        AvaloniaProperty.Register<TabsView, double>(nameof(RightIndent),
            defaultValue: OperatingSystem.IsWindows() ? WindowsDefaultRightThumbWidth : MacOsDefaultRightThumbWidth);

    public static readonly StyledProperty<object?> LeftContentProperty =
        AvaloniaProperty.Register<TabsView, object?>(nameof(LeftContent));

    public static readonly StyledProperty<object?> LeftHeaderContentProperty =
        AvaloniaProperty.Register<TabsView, object?>(nameof(LeftHeaderContent));
    
    public static readonly StyledProperty<object?> RightHeaderContentProperty =
        AvaloniaProperty.Register<TabsView, object?>(nameof(RightHeaderContent));

    public static readonly StyledProperty<bool> LeftHeaderContentDraggableProperty =
        AvaloniaProperty.Register<TabsView, bool>(nameof(LeftHeaderContentDraggable));
    
    public static readonly StyledProperty<bool> RightHeaderContentDraggableProperty =
        AvaloniaProperty.Register<TabsView, bool>(nameof(RightHeaderContentDraggable));

    public static readonly StyledProperty<InterTabController?> InterTabControllerProperty =
        AvaloniaProperty.Register<TabsView, InterTabController?>(nameof(InterTabController), InterTabController.Default);

    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        ContentControl.HorizontalContentAlignmentProperty.AddOwner<TabsView>();

    public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
        ContentControl.VerticalContentAlignmentProperty.AddOwner<TabsView>();
    
    #endregion

    #region Constructor

    public TabsView()
    {
        AddHandler(TabsViewItem.MiddleClick, ItemMiddleClick, handledEventsToo: true);
        AddHandler(TabsViewItem.DragStarted, ItemDragStarted, handledEventsToo: true);
        AddHandler(TabsViewItem.DragDelta, ItemDragDelta);
        AddHandler(TabsViewItem.DragCompleted, ItemDragCompleted, handledEventsToo: true);

        _tabsPanel = new TabsPanel(this)
        {
            ItemWidth = TabItemWidth,
            ItemOffset = AdjacentHeaderItemOffset
        };

        _tabsPanel.DragCompleted += TabsPanelOnDragCompleted;

        AddItemCommand = new SimpleActionCommand(AddItem);
        CloseItemCommand = new SimpleParamActionCommand(CloseItem);
    }

    private void ItemMiddleClick(object? sender, RoutedEventArgs e)
    {
        CloseItem(e.Source);
    }

    #endregion

    #region Public Properties
    
    public double AdjacentHeaderItemOffset
    {
        get => GetValue(AdjacentHeaderItemOffsetProperty);
        set => SetValue(AdjacentHeaderItemOffsetProperty, value);
    }

    public double TabItemWidth
    {
        get => GetValue(TabItemWidthProperty);
        set => SetValue(TabItemWidthProperty, value);
    }

    public bool ShowDefaultCloseButton
    {
        get => GetValue(ShowDefaultCloseButtonProperty);
        set => SetValue(ShowDefaultCloseButtonProperty, value);
    }

    public bool ShowDefaultAddButton
    {
        get => GetValue(ShowDefaultAddButtonProperty);
        set => SetValue(ShowDefaultAddButtonProperty, value);
    }

    public event EventHandler<TabsView, EventArgs>? AddTabButtonClick;

    public event EventHandler<TabsView, TabClosingEventArgs>? TabClosing;

    public event EventHandler<TabsView, TabClosedEventArgs>? TabClosed;

    /// <summary>
    /// Allows a first adjacent tabs to be fixed (no dragging, and default close button will not show).
    /// </summary>
    public int FixedHeaderCount
    {
        get => GetValue(FixedHeaderCountProperty);
        set => SetValue(FixedHeaderCountProperty, value);
    }

    public double LeftIndent
    {
        get => GetValue(LeftIndentProperty);
        set => SetValue(LeftIndentProperty, value);
    }

    public double RightIndent
    {
        get => GetValue(RightIndentProperty);
        set => SetValue(RightIndentProperty, value);
    }

    public ICommand AddItemCommand { get; }

    public ICommand CloseItemCommand { get; }

    public object? LeftContent
    {
        get => GetValue(LeftContentProperty);
        set => SetValue(LeftContentProperty, value);
    }

    public object? LeftHeaderContent
    {
        get => GetValue(LeftHeaderContentProperty);
        set => SetValue(LeftHeaderContentProperty, value);
    }
    
    public object? RightHeaderContent
    {
        get => GetValue(RightHeaderContentProperty);
        set => SetValue(RightHeaderContentProperty, value);
    }

    public bool LeftHeaderContentDraggable
    {
        get => GetValue(LeftHeaderContentDraggableProperty);
        set => SetValue(LeftHeaderContentDraggableProperty, value);
    }
    
    public bool RightHeaderContentDraggable
    {
        get => GetValue(RightHeaderContentDraggableProperty);
        set => SetValue(RightHeaderContentDraggableProperty, value);
    }

    /// <summary>
    /// Configuration for inter-tab (cross-window) dragging. Set to a non-null value to enable
    /// tearing tabs out to new windows and merging tabs between windows.
    /// </summary>
    public InterTabController? InterTabController
    {
        get => GetValue(InterTabControllerProperty);
        set => SetValue(InterTabControllerProperty, value);
    }

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    public VerticalAlignment VerticalContentAlignment
    {
        get => GetValue(VerticalContentAlignmentProperty);
        set => SetValue(VerticalContentAlignmentProperty, value);
    }

    public object? SelectedItem
    {
        get => _tabControl?.SelectedItem ?? SelectedPage;
        set => SelectItem(value);
    }

    public TabsViewItem? SelectedTabItem => _tabControl?.ContainerFromIndex(SelectedIndex) as TabsViewItem;
    
    #endregion

    #region Protected Methods

    protected override Type StyleKeyOverride => typeof(TabsView);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (e.NameScope.Find<DragTabControl>("PART_TabControl") is { } tabControl)
            ConfigureTabControl(tabControl);
    }

    internal void OnTabControlTemplateApplied(DragTabControl tabControl, TemplateAppliedEventArgs e)
    {
        if (!ReferenceEquals(_tabControl, tabControl))
            ConfigureTabControl(tabControl);

        AddDragHandler(e.NameScope.Find<Rectangle>("PART_LeftDragWindowThumb"));
        AddDragHandler(e.NameScope.Find<Rectangle>("PART_RightDragWindowThumb"));
        AddDragHandler(e.NameScope.Find<Rectangle>("PART_MiddleDragWindowThumb"));
        _leftHeaderContentDraggable = e.NameScope.Find<ContentPresenter>("PART_LeftHeaderContent");
        if (LeftHeaderContentDraggable)
            AddDragHandler(_leftHeaderContentDraggable);
        _rightHeaderContentDraggable = e.NameScope.Find<ContentPresenter>("PART_RightHeaderContent");
        if (RightHeaderContentDraggable)
            AddDragHandler(_rightHeaderContentDraggable);
    }

    private void ConfigureTabControl(DragTabControl tabControl)
    {
        _tabControl = tabControl;
        tabControl.Owner = this;
        tabControl.ItemsPanel = new FuncTemplate<Panel?>(() => _tabsPanel);
        tabControl.ApplyTemplate();
    }

    private void AddDragHandler(Control? control)
    {
        if (control is null)
            return;

        control.AddHandler(PointerPressedEvent, OnThumbBeginDrag, handledEventsToo: true);
        control.DoubleTapped += WindowDragThumbOnDoubleTapped;
    }

    private void RemoveDragHandler(Control? control)
    {
        if (control is null)
            return;

        control.RemoveHandler(PointerPressedEvent, OnThumbBeginDrag);
        control.DoubleTapped -= WindowDragThumbOnDoubleTapped;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == AdjacentHeaderItemOffsetProperty)
        {
            _tabsPanel.ItemOffset = AdjacentHeaderItemOffset;
        }
        else if (change.Property == TabItemWidthProperty)
        {
            _tabsPanel.ItemWidth = TabItemWidth;
            _tabsPanel.InvalidateMeasure();
        }
        else if (change.Property == LeftHeaderContentDraggableProperty)
        {
            if (LeftHeaderContentDraggable)
                AddDragHandler(_leftHeaderContentDraggable);
            else
                RemoveDragHandler(_leftHeaderContentDraggable);
        }
        else if (change.Property == RightHeaderContentDraggableProperty)
        {
            if (RightHeaderContentDraggable)
                AddDragHandler(_rightHeaderContentDraggable);
            else
                RemoveDragHandler(_rightHeaderContentDraggable);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _LoadedInstances.Add(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _LoadedInstances.Remove(this);
        base.OnDetachedFromVisualTree(e);
    }

    #endregion

    private int ItemCount
    {
        get
        {
            if (_tabControl is not null)
                return _tabControl.ItemCount;

            var source = (IEnumerable?) ItemsSource ?? Pages;
            if (source is ICollection collection)
                return collection.Count;

            var count = 0;
            if (source is not null)
                foreach (var _ in source)
                    count++;

            return count;
        }
    }

    private void SetSelectedNewTab(IList items, int removedItemIndex) =>
        SelectedIndex = removedItemIndex == items.Count ? items.Count - 1 : removedItemIndex;

    private IEnumerable<TabsViewItem> DragTabItems()
    {
        if (_tabControl is null)
            yield break;

        for (var i = 0; i < _tabControl.ItemCount; i++)
        {
            if (_tabControl.ContainerFromIndex(i) is TabsViewItem dragTabItem)
                yield return dragTabItem;
        }
    }

    private object? ItemFromContainer(TabsViewItem container) =>
        _tabControl?.ItemFromContainer(container);

    private TabsViewItem? ContainerFromItem(object item) =>
        _tabControl?.ContainerFromItem(item) as TabsViewItem;

    private bool TryRemoveItemForTransfer(object item, TabsViewItem? container, out int removedIndex)
    {
        removedIndex = IndexOfItem(item);
        if (removedIndex is -1)
            return false;

        ReleaseTransferredContainer(container);
        SelectTransferReplacement(item, removedIndex);

        return RemoveSourceItem(item);
    }

    private void SelectTransferReplacement(object item, int removedIndex)
    {
        if (!Equals(SelectedItem, item))
            return;

        if (ItemCount < 2)
        {
            _tabControl?.SelectedIndex = -1;
            return;
        }

        SelectedIndex = removedIndex >= ItemCount - 1
            ? removedIndex - 1
            : removedIndex + 1;
    }

    private static void ReleaseTransferredContainer(TabsViewItem? container)
    {
        if (container is null)
            return;

        container.ClearReusableContent();
        container.IsDragging = false;
        container.IsSiblingDragging = false;
        container.X = 0;
        container.Y = 0;
    }

    private object? ItemAt(int index)
    {
        if (_tabControl is { } tabControl)
        {
            var itemsView = tabControl.ItemsView;
            if ((uint) index < (uint) itemsView.Count)
                return itemsView[index];
        }

        var source = (IEnumerable?) ItemsSource ?? Pages;
        if (source is null)
            return null;

        var currentIndex = 0;
        foreach (var item in source)
        {
            if (currentIndex == index)
                return item;

            currentIndex++;
        }

        return null;
    }

    private int IndexOfItem(object? item)
    {
        if (item is null)
            return -1;

        if (_tabControl is { } tabControl)
            return tabControl.ItemsView.IndexOf(item);

        var source = (IEnumerable?) ItemsSource ?? Pages;
        if (source is IList list)
            return list.IndexOf(item);

        if (source is null)
            return -1;

        var index = 0;
        foreach (var candidate in source)
        {
            if (Equals(candidate, item))
                return index;

            index++;
        }

        return -1;
    }

    private bool TryGetMutableItems(out IList items)
    {
        if (ItemsSource is IList itemsSource)
        {
            items = itemsSource;
            return true;
        }

        if (Pages is IList pages)
        {
            items = pages;
            return true;
        }

        items = null!;
        return false;
    }

    private void SelectItem(object? item)
    {
        if (_tabControl is not null)
        {
            _tabControl.SelectedItem = item;
            return;
        }

        SelectedIndex = IndexOfItem(item);
    }

    private bool RemoveSourceItem(object? item)
    {
        if (item is null || !TryGetMutableItems(out var items))
            return false;

        items.Remove(item);
        return true;
    }

    private bool InsertSourceItem(int index, object item)
    {
        if (!TryGetMutableItems(out var items))
            return false;

        if (index >= items.Count)
            items.Add(item);
        else
            items.Insert(index, item);

        return true;
    }

    private bool AddSourceItem(object item) => InsertSourceItem(ItemCount, item);

    private void ItemDragStarted(object? sender, DragTabDragStartedEventArgs e)
    {
        _draggedItem = e.TabItem;

        e.Handled = true;

        _draggedItem.IsSelected = true;

        // Record pointer position relative to the tab at drag start.
        // This is used later by MonitorBreach so that the new window places
        // the tab under the cursor at the same offset as the original grab.
        var thumb = _draggedItem.FindDescendantOfType<LeftPressedThumb>();
        if (thumb is not null)
        {
            try
            {
                var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
                var cursorScreen = thumb.LastScreenPosition;
                var tabScreen = _draggedItem.PointToScreen(new Point(0, 0));
                _dragStartPointerInTab = new Point(
                    (cursorScreen.X - tabScreen.X) / scaling,
                    (cursorScreen.Y - tabScreen.Y) / scaling);
            }
            catch
            {
                _dragStartPointerInTab = new Point(_draggedItem.Bounds.Width / 2, _draggedItem.Bounds.Height / 2);
            }
        }

        if (ItemFromContainer(_draggedItem) is { } item)
        {
            if (item is TabItem tabItem)
                tabItem.IsSelected = true;

            SelectedItem = item;
        }
    }

    private void ItemDragDelta(object? sender, DragTabDragDeltaEventArgs e)
    {
        if (_draggedItem is null || _floatingState is not null)
        {
            // After tear-off, _draggedItem is null and pointer tracking
            // is handled by OnPointerMoved instead of DragDelta events.
            e.Handled = true;
            return;
        }

        if (_draggedItem.LogicalIndex < FixedHeaderCount)
        {
            e.Handled = true;
            return;
        }

        // Single-tab windows: immediately start window drag for merge capability
        if (ItemCount < 2 && InterTabController is not null)
        {
            StartSingleTabWindowDrag(e);
            e.Handled = true;
            return;
        }

        if (!_dragging)
        {
            _dragging = true;
            SetDraggingItem(_draggedItem);
        }

        _draggedItem.X += e.DragDeltaEventArgs.Vector.X;
        _draggedItem.Y += e.DragDeltaEventArgs.Vector.Y;

        if (InterTabController is { } controller)
        {
            // Check if pointer has breached the tab header boundary → tear off
            MonitorBreach(e.PointerScreenPosition, controller, false);
        }

        Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);

        e.Handled = true;
    }

    private void ItemDragCompleted(object? sender, DragTabDragCompletedEventArgs e)
    {
        if (_floatingState is not null)
        {
            // DragCompleted fired because we transferred capture from LeftPressedThumb
            // to TabsControl in MonitorBreach. Don't do normal cleanup — MonitorBreach
            // is still running and OnPointerMoved/Released will handle the rest.
            e.Handled = true;
            return;
        }

        foreach (var item in DragTabItems())
        {
            item.IsDragging = false;
            item.IsSiblingDragging = false;
        }

        Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);

        _dragging = false;
    }

    private void SetDraggingItem(TabsViewItem draggedItem)
    {
        foreach (var item in DragTabItems())
        {
            item.IsDragging = false;
            item.IsSiblingDragging = true;
        }

        draggedItem.IsDragging = true;
        draggedItem.IsSiblingDragging = false;
    }

    private void TabsPanelOnDragCompleted()
    {
        MoveTabModelsIfNeeded();

        _draggedItem = null;
    }

    private void MoveTabModelsIfNeeded()
    {
        var draggedItem = _draggedItem!;

        if (ItemFromContainer(draggedItem) is not { } item)
            return;
        if (!TryGetMutableItems(out var list))
            return;
        if (draggedItem.LogicalIndex == list.IndexOf(item))
            return;
        list.Remove(item);
        list.Insert(int.Min(draggedItem.LogicalIndex, list.Count), item);

        SelectedItem = item;

        var i = 0;

        foreach (var dragTabItem in DragTabItems())
        {
            dragTabItem.LogicalIndex = i;
            i++;
        }
    }

    private void OnThumbBeginDrag(object? sender, PointerPressedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is not Window window)
            return;
        var currentPoint = e.GetCurrentPoint(this);
        if (currentPoint.Properties.IsLeftButtonPressed ||
            currentPoint.Pointer.Type is PointerType.Touch)
        {
            window.BeginMoveDrag(e);
        }
    }

    private void WindowDragThumbOnDoubleTapped(object? sender, RoutedEventArgs e)
    {
        var window = this.FindLogicalAncestorOfType<Window>();

        window?.RestoreWindow();
    }

    private void AddItem() => AddTabButtonClick?.Invoke(this, EventArgs.Empty);

    private void CloseItem(object? tabItemSource)
    {
        ArgumentNullException.ThrowIfNull(tabItemSource);

        if (tabItemSource is not TabsViewItem tabItem)
            return;

        RemoveItem(tabItem);
    }

    private void RemoveItem(TabsViewItem container)
    {
        var item = ItemFromContainer(container);
        var removedItemIndex = IndexOfItem(item);
        if (removedItemIndex is -1)
            return;

        TabClosingEventArgs tabClosingEventArgs = new(item, container);
        TabClosing?.Invoke(this, tabClosingEventArgs);
        if (tabClosingEventArgs.Cancel)
            return;

        var removedItemIsSelected = SelectedItem == item;
        if (!RemoveSourceItem(item))
            return;

        var closedEventArgs = new TabClosedEventArgs(item, container);
        TabClosed?.Invoke(this, closedEventArgs);
        if (ItemCount is 0)
        {
            this.FindLogicalAncestorOfType<Window>()?.Close();
        }
        else if (removedItemIsSelected)
        {
            if (TryGetMutableItems(out var items))
                SetSelectedNewTab(items, removedItemIndex);
        }
    }

    #region Inter-Tab Drag Support

    /// <summary>
    /// Checks whether the pointer's screen position (<paramref name="screenPoint"/>)
    /// is inside this <see cref="TabsView"/>'s tab header row area.
    /// Uses the TopPanel's full bounds so that empty space in the tab strip is also a valid drop zone.
    /// </summary>
    internal bool IsPointInTabHeaderArea(PixelPoint screenPoint)
    {
        if (!IsVisible || !IsArrangeValid)
            return false;

        return TryGetPointInHeaderArea(screenPoint, out var point, out var headerArea)
            && point.X >= 0
            && point.X <= headerArea.Bounds.Width
            && point.Y >= 0
            && point.Y <= headerArea.Bounds.Height;
    }

    private bool IsPointOutsideTabHeaderArea(PixelPoint screenPoint, double horizontalGrace, double verticalGrace)
    {
        return TryGetPointInHeaderArea(screenPoint, out var point, out var headerArea)
            && (point.X < -horizontalGrace
                || point.X > headerArea.Bounds.Width + horizontalGrace
                || point.Y < -verticalGrace
                || point.Y > headerArea.Bounds.Height + verticalGrace);
    }

    private bool TryGetPointInHeaderArea(PixelPoint screenPoint, out Point point, out Control headerArea)
    {
        headerArea = _tabsPanel.FindAncestorOfType<TopPanel>() as Control ?? _tabsPanel;
        point = default;

        if (!headerArea.IsArrangeValid)
            return false;

        try
        {
            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var origin = headerArea.PointToScreen(new Point(0, 0));
            point = new Point(
                (screenPoint.X - origin.X) / scaling,
                (screenPoint.Y - origin.Y) / scaling);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// When only one tab remains, dragging it moves the entire window.
    /// This allows the user to merge a single-tab window into another window
    /// by dragging it over the target's tab header area.
    /// </summary>
    private void StartSingleTabWindowDrag(DragTabDragDeltaEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is not Window sourceWindow)
            return;

        var draggedItem = _draggedItem!;

        // Get the pointer from LeftPressedThumb so we can transfer capture
        var thumb = draggedItem.FindDescendantOfType<LeftPressedThumb>();
        var pointer = thumb?.CapturedPointer;
        if (pointer is null)
            return;

        var cursorScreen = e.PointerScreenPosition;
        var dragOffset = new Point(
            cursorScreen.X - sourceWindow.Position.X,
            cursorScreen.Y - sourceWindow.Position.Y);

        // Set floating state BEFORE capture transfer (same pattern as MonitorBreach).
        // pointer.Capture(this) will synchronously fire DragCompleted on LeftPressedThumb;
        // ItemDragCompleted checks _floatingState and returns early.
        _floatingState = new FloatingDragState
        {
            FloatingWindow = sourceWindow,
            FloatingTabsControl = this,
            DragOffset = dragOffset
        };

        // Transfer pointer capture: LeftPressedThumb → TabsControl
        pointer.Capture(this);

        // Reset tab drag state — the tab is no longer "being dragged" visually
        _draggedItem = null;
        _dragging = false;
        foreach (var di in DragTabItems())
        {
            di.IsDragging = false;
            di.IsSiblingDragging = false;
            di.X = 0;
            di.Y = 0;
        }

        Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Detects if the pointer has moved outside the tab header area by the configured grace distance.
    /// If so, initiates a tear-off: transfers pointer capture to this <see cref="TabsView"/>,
    /// removes the tab from source, creates a floating window, and adds the tab to it.
    /// From that point on, <see cref="OnPointerMoved"/> and <see cref="OnPointerReleased"/>
    /// handle floating window tracking and merge completion.
    /// </summary>
    /// 
    /// <remarks>
    /// Checks if the pointer has moved outside the tab header area during a continued
    /// merged-item drag. If so, tears the tab off into a new floating window.
    /// Similar to <see cref="MonitorBreach"/> but works when the pointer is already
    /// captured to this <see cref="TabsView"/> (not to a <see cref="LeftPressedThumb"/>).
    /// </remarks>
    private void MonitorBreach(PixelPoint cursorScreen, InterTabController controller, bool fromMergedDrag)
    {
        if (ItemCount < 2)
            return; // Don't tear off the last tab; single-tab windows use native window drag.

        if (!IsPointOutsideTabHeaderArea(
                cursorScreen,
                controller.HorizontalPopoutGrace,
                controller.VerticalPopoutGrace))
            return;

        // === Breach detected: initiate tear-off (Edge-style) ===
        if (TopLevel.GetTopLevel(this) is not Window sourceWindow)
            return;

        var draggedItem = _draggedItem!;

        // Get the data item to transfer
        if (ItemFromContainer(draggedItem) is not { } item)
            return;

        IPointer? pointer = null;
        if (!fromMergedDrag)
        {
            // Get the pointer from LeftPressedThumb so we can transfer capture
            var thumb = draggedItem.FindDescendantOfType<LeftPressedThumb>();
            pointer = thumb?.CapturedPointer;
            if (pointer is null)
                return;
        }

        // Use the pointer-in-tab offset captured at drag start, so the
        // new window positions the tab under the cursor at the original grab point.
        var pointerInTab = _dragStartPointerInTab;

        // In the new window the tab will be offset from the window's top-left by
        // the same header structure (LeftThumb + LeftHeaderContent).  Estimate that offset
        // from the source layout so we can place the window correctly.
        Point tabOriginInWindow;
        try
        {
            var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
            var panelScreenOrigin = _tabsPanel.PointToScreen(new Point(0, 0));
            tabOriginInWindow = new Point(
                (panelScreenOrigin.X - sourceWindow.Position.X) / scaling,
                (panelScreenOrigin.Y - sourceWindow.Position.Y) / scaling);
        }
        catch
        {
            tabOriginInWindow = new Point(LeftIndent, 0);
        }

        var sourceScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        // dragOffset (in pixels) = distance from new window top-left to cursor
        var dragOffset = new Point(
            (tabOriginInWindow.X + pointerInTab.X) * sourceScaling,
            (tabOriginInWindow.Y + pointerInTab.Y) * sourceScaling);

        double newWidth, newHeight;
        if (sourceWindow.WindowState is WindowState.Maximized)
        {
            newWidth = double.Min(800, sourceWindow.Bounds.Width);
            newHeight = double.Min(600, sourceWindow.Bounds.Height);
        }
        else
        {
            newWidth = sourceWindow.Width;
            newHeight = sourceWindow.Height;
        }

        // Create the new host window
        TabsHost newTabHost;
        try
        {
            newTabHost = controller.InterTabClient.GetNewHost(controller, new TabsHost(sourceWindow, this));
        }
        catch
        {
            return; // client couldn't create a host; silently abort
        }

        var floatingWindow = newTabHost.Window;
        var floatingTabsControl = newTabHost.TabsControl;

        // Propagate inter-tab configuration so the new window can also tear off & merge
        floatingTabsControl.InterTabController ??= new InterTabController
        {
            InterTabClient = controller.InterTabClient,
            Partition = controller.Partition,
            HorizontalPopoutGrace = controller.HorizontalPopoutGrace,
            VerticalPopoutGrace = controller.VerticalPopoutGrace
        };

        floatingWindow.Width = newWidth;
        floatingWindow.Height = newHeight;
        floatingWindow.WindowStartupLocation = WindowStartupLocation.Manual;
        floatingWindow.Position = new PixelPoint(
            (int)(cursorScreen.X - dragOffset.X),
            (int)(cursorScreen.Y - dragOffset.Y));

        // *** Set floating state BEFORE capture transfer ***
        // pointer.Capture(this) will synchronously fire DragCompleted on LeftPressedThumb,
        // which bubbles to ItemDragCompleted. That handler checks _floatingState and returns early.
        _floatingState = new FloatingDragState
        {
            FloatingWindow = floatingWindow,
            FloatingTabsControl = floatingTabsControl,
            DragOffset = dragOffset
        };

        if (!fromMergedDrag)
            // Transfer pointer capture from LeftPressedThumb → TabsControl.
            // This fires DragCompleted on the thumb synchronously (handled above).
            // After this, OnPointerMoved / OnPointerReleased on TabsControl drive the floating drag.
            pointer!.Capture(this);

        // Release the old presenters before the page is inserted into a different TabControl.
        if (!TryRemoveItemForTransfer(item, draggedItem, out var removedIndex))
            return;

        // Reset drag state on the source: the item is gone
        _draggedItem = null;
        _dragging = false;
        foreach (var di in DragTabItems())
        {
            di.IsDragging = false;
            di.IsSiblingDragging = false;
        }

        // Handle source remaining tabs selection (or close if empty)
        HandleSourceEmptied(removedIndex);

        // Show the floating window and add the item after layout is ready
        floatingWindow.Show();

        // Force source layout recalculation for remaining tabs
        InvalidateTabLayout();

        Dispatcher.UIThread.Post(() =>
        {
            _ = floatingTabsControl.AddSourceItem(item);
            floatingTabsControl.SelectedItem = item;
            floatingTabsControl.InvalidateTabLayout();
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Tracks the floating window position while the pointer is captured to this TabsControl.
    /// When the floating window enters another TabsControl's tab header area, immediately
    /// merges the tab into the target and transfers drag control there.
    /// </summary>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_floatingState is { } fs)
        {
            HandleFloatingPointerMoved(e, fs);
            return;
        }

        // Continue-drag after merge: _draggedItem is set and pointer is captured to this TC
        if (_draggedItem is not null && _dragging)
        {
            HandleMergedDragPointerMoved(e);
        }
    }

    private void HandleFloatingPointerMoved(PointerEventArgs e, FloatingDragState fs)
    {
        PixelPoint cursorScreen;
        try
        {
            cursorScreen = this.PointToScreen(e.GetPosition(this));
        }
        catch
        {
            return;
        }

        fs.FloatingWindow.Position = new PixelPoint(
            (int)(cursorScreen.X - fs.DragOffset.X),
            (int)(cursorScreen.Y - fs.DragOffset.Y));

        // Check if we're over a merge target
        var mergeTarget = FindReentryTarget(cursorScreen, InterTabController?.Partition, fs.FloatingTabsControl);
        if (mergeTarget is null)
            return;

        // === Immediate merge: transfer the tab and continue dragging in the target ===
        var sourceTC = fs.FloatingTabsControl;
        if (sourceTC.ItemCount == 0)
            return;

        var item = sourceTC.ItemAt(0);
        var sourceContainer = item is not null ? sourceTC.ContainerFromItem(item) : null;
        if (item is null || !sourceTC.TryRemoveItemForTransfer(item, sourceContainer, out _))
            return;

        // Clear floating state on source (us) BEFORE releasing capture
        var windowToClose = fs.FloatingWindow;
        _floatingState = null;

        // Release capture from source TabsControl
        e.Pointer.Capture(null);

        // All operations on mergeTarget must be deferred because it belongs
        // to a different window with its own LayoutManager. Performing layout
        // invalidation synchronously here would cause
        // "InvalidateArrange on wrong LayoutManager".
        var pointer = e.Pointer;
        Dispatcher.UIThread.Post(() =>
        {
            // Activate the target window (bring to front)
            if (TopLevel.GetTopLevel(mergeTarget) is Window targetWindow)
                targetWindow.Activate();

            // Close the now-empty floating window
            windowToClose.Close();

            // Start dragging the merged tab in the target TabsControl.
            // ReceiveMergedItemDrag handles source insertion, layout, and pointer capture.
            mergeTarget.ReceiveMergedItemDrag(item, pointer, cursorScreen, _dragStartPointerInTab);
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Handles pointer movement during a continued drag after merge.
    /// Moves the dragged tab horizontally and checks for re-tear-off.
    /// </summary>
    private void HandleMergedDragPointerMoved(PointerEventArgs e)
    {
        var draggedItem = _draggedItem!;
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        PixelPoint cursorScreen;
        try
        {
            cursorScreen = this.PointToScreen(e.GetPosition(this));
        }
        catch
        {
            return;
        }

        // Compute new X for the dragged tab (relative to TabsPanel)
        try
        {
            var panelScreen = _tabsPanel.PointToScreen(new Point(0, 0));
            draggedItem.X = ((cursorScreen.X - panelScreen.X) / scaling) - _dragStartPointerInTab.X;
        }
        catch
        {
            // Can't compute; leave as-is
        }

        // Check if pointer has breached the header boundary → re-tear-off
        if (InterTabController is { } controller)
        {
            MonitorBreach(cursorScreen, controller, true);
        }

        Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Completes the floating drag when the pointer is released.
    /// The floating window stays as a new independent window (merge happens
    /// immediately in <see cref="OnPointerMoved"/> when entering a target).
    /// </summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_floatingState is not null)
        {
            _floatingState = null;
            e.Handled = true;
            e.Pointer.Capture(null);
            return;
        }

        // Complete a continued-drag after merge
        if (_draggedItem is not null && _dragging)
        {
            e.Pointer.Capture(null);
            e.Handled = true;

            foreach (var item in DragTabItems())
            {
                item.IsDragging = false;
                item.IsSiblingDragging = false;
            }

            _dragging = false;

            Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);
        }
    }

    /// <summary>
    /// Handles unexpected capture loss during a floating drag.
    /// Leaves the floating window as-is (it already has the tab).
    /// </summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_floatingState is not { } fs)
            return;

        _floatingState = null;
        fs.FloatingWindow.Opacity = 1.0;
    }

    /// <summary>
    /// Accepts a merged tab item and continues dragging it within this <see cref="TabsView"/>.
    /// The pointer is captured to this control, and <see cref="OnPointerMoved"/> /
    /// <see cref="OnPointerReleased"/> handle the ongoing drag (including re-tear-off via MonitorBreach).
    /// </summary>
    /// <param name="item"></param>
    /// <param name="pointer"></param>
    /// <param name="cursorScreen"></param>
    /// <param name="pointerInTab">The pointer offset inside the tab at drag start (DIPs), from the source.</param>
    internal void ReceiveMergedItemDrag(object item, IPointer pointer, PixelPoint cursorScreen, Point pointerInTab)
    {
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;

        // Use the original pointer-in-tab offset from the source so the
        // relative cursor-to-tab position stays consistent across merges.
        _dragStartPointerInTab = pointerInTab;

        // Calculate cursor X relative to the TabsPanel to determine the insert index.
        double cursorXInPanel = 0;
        try
        {
            var panelScreen = _tabsPanel.PointToScreen(new Point(0, 0));
            cursorXInPanel = (cursorScreen.X - panelScreen.X) / scaling;
        }
        catch
        {
            // Fall through — insert at end
        }

        // Find the best insertion index based on cursor position among existing tabs.
        var insertIndex = CalculateInsertIndex(cursorXInPanel);

        // Insert the item at the computed position (instead of appending at end)
        _ = InsertSourceItem(insertIndex, item);

        SelectedItem = item;

        // Force a synchronous layout pass so that TabsPanel recalculates _itemWidth
        // for the new tab count. Without this, the tab widths reflect the old count
        // and overflow under the window control buttons.
        InvalidateTabLayout();
        UpdateLayout();

        // Now find the container after layout is complete
        if (ContainerFromItem(item) is not TabsViewItem container)
            return;

        // Compute the X position of the tab so it appears under the cursor at the grab point
        try
        {
            var panelScreen = _tabsPanel.PointToScreen(new Point(0, 0));
            container.X = ((cursorScreen.X - panelScreen.X) / scaling) - _dragStartPointerInTab.X;
        }
        catch
        {
            // Leave X at default (0) if we can't compute
        }

        // Set up drag state on this TabsControl
        _draggedItem = container;
        _dragging = true;
        container.IsSelected = true;

        // Set drag visual state
        foreach (var di in DragTabItems())
        {
            di.IsDragging = false;
            di.IsSiblingDragging = true;
        }
        container.IsDragging = true;
        container.IsSiblingDragging = false;

        // Capture pointer to this TabsControl so OnPointerMoved/Released drive the drag.
        // This makes the merged tab follow the cursor and enables re-tear-off.
        pointer.Capture(this);

        Dispatcher.UIThread.Post(_tabsPanel.InvalidateMeasure, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Calculates the insertion index for a tab being merged, based on the cursor's
    /// horizontal position within the <see cref="TabsPanel"/>.
    /// </summary>
    private int CalculateInsertIndex(double cursorXInPanel)
    {
        var index = 0;
        foreach (var di in DragTabItems())
        {
            var tabMid = di.X + (di.Bounds.Width / 2);
            if (cursorXInPanel > tabMid)
                index = di.LogicalIndex + 1;
        }
        return index;
    }

    /// <summary>
    /// Finds a <see cref="TabsView"/> instance (in any window) whose tab header area
    /// contains the given screen point and belongs to the same partition.
    /// </summary>
    /// <param name="screenPoint">Pointer position in screen coordinates.</param>
    /// <param name="partition">Partition string to match (null matches null).</param>
    /// <param name="exclude">A <see cref="TabsView"/> to exclude (e.g. the floating window's TC).</param>
    private static TabsView? FindReentryTarget(PixelPoint screenPoint, string? partition, TabsView? exclude)
    {
        foreach (var tc in _LoadedInstances)
        {
            if (tc == exclude)
                continue;

            // Partition matching: both must be null, or the same string
            var tcPartition = tc.InterTabController?.Partition;
            if (tcPartition != partition)
                continue;

            if (tc.IsPointInTabHeaderArea(screenPoint))
                return tc;
        }

        return null;
    }

    /// <summary>
    /// Handles the scenario where the source <see cref="TabsView"/> becomes empty after a tab transfer.
    /// </summary>
    private void HandleSourceEmptied(int removedIndex)
    {
        if (ItemCount > 0)
        {
            SelectedIndex = removedIndex >= 0 && removedIndex < ItemCount
                ? removedIndex
                : ItemCount - 1;
            return;
        }

        if (TopLevel.GetTopLevel(this) is not Window window)
            return;

        Dispatcher.UIThread.Post(window.Close);
    }

    /// <summary>
    /// Forces the TabsPanel and its TopPanel ancestor to re-measure and re-arrange.
    /// Called after inter-tab item operations to ensure correct tab widths.
    /// </summary>
    private void InvalidateTabLayout()
    {
        _tabsPanel.InvalidateMeasure();
        if (_tabsPanel.Parent is Layoutable presenter)
        {
            presenter.InvalidateMeasure();
            if (presenter.Parent is Layoutable topPanel)
                topPanel.InvalidateMeasure();
        }
    }

    /// <summary>
    /// Mutable state held on the SOURCE <see cref="TabsView"/> during a tear-off drag.
    /// The source retains pointer capture and uses this state to move the floating window
    /// and detect merge targets.
    /// </summary>
    private sealed class FloatingDragState
    {
        /// <summary>The floating window created by the tear-off, or the source window for single-tab drag.</summary>
        public required Window FloatingWindow { get; init; }

        /// <summary>The <see cref="TabsView"/> inside the floating window, or this TC for single-tab drag.</summary>
        public required TabsView FloatingTabsControl { get; init; }

        /// <summary>
        /// Pixel offset from the pointer to the floating window's top-left corner,
        /// preserved so the window follows the pointer smoothly.
        /// </summary>
        public required Point DragOffset { get; init; }
    }

    #endregion
}
