using System;
using System.Collections.Generic;
using System.Linq;

namespace TabView.Avalonia.Primitives;

public class TabsPanel(TabsView tabsControl) : Panel
{
    #region Private Fields

    private readonly Dictionary<TabsViewItem, LocationInfo> _itemsLocations = [];
    private double _itemWidth;
    private readonly Dictionary<TabsViewItem, double> _activeStoryboardTargetLocations = [];
    private TabsViewItem? _dragItem;
    
    #endregion

    public event Action? DragCompleted;

    #region Public Properties

    public double ItemWidth { get; internal set; }

    public double ItemOffset { get; internal set; }
    
    #endregion

    #region Protected Methods
    
    protected override Size MeasureOverride(Size availableSize)
    {
        var draggedItem = GetDragItem();

        return draggedItem is not null 
            ? DragMeasureImpl(draggedItem, availableSize) 
            : MeasureImpl(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        UpdateTabSeparators();
        var draggedItem = GetDragItem();
            
        if (_dragItem is not null && draggedItem is null)
        {
            var oldDragItem = _dragItem;
            _dragItem = null;
                
            return DragCompletedArrangeImpl(oldDragItem, finalSize);
        }
            
        _dragItem = draggedItem;
            
        return draggedItem is not null
            ? DragArrangeImpl(draggedItem, finalSize) 
            : ArrangeImpl(finalSize);
    }
        
    #endregion

    internal void UpdateTabSeparators()
    {
        // WinUI hides both sides of a hovered/selected tab, and the trailing divider.
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i] is not TabsViewItem item)
                continue;
            var next = i + 1 < Children.Count ? Children[i + 1] as TabsViewItem : null;
            item.SetSeparatorHidden(next is null || item.IsSelected || item.IsPointerOver || item.IsDragging ||
                next.IsSelected || next.IsPointerOver || next.IsDragging);
        }
    }

    private Size MeasureImpl(Size availableSize)
    {
        _itemWidth = GetAvailableWidth(availableSize);

        double height = 0;
        double width = 0;

        var isFirst = true;

        foreach (var tabItem in Children)
        {
            tabItem.Measure(new Size(_itemWidth, availableSize.Height));

            width += _itemWidth;
            height = double.Max(tabItem.DesiredSize.Height, height);

            if (!isFirst)
                width += ItemOffset;

            isFirst = false;
        }

        return new Size(width, height);
    }

    private Size DragMeasureImpl(TabsViewItem draggedItem, Size availableSize)
    {
        double height = 0;
        double width = 0;

        var isFirst = true;

        foreach (var tabItem in Children)
        {
            tabItem.Measure(new Size(_itemWidth, availableSize.Height));
                
            width += _itemWidth;
            height = double.Max(tabItem.DesiredSize.Height, height);

            if (!isFirst)
                width += ItemOffset;

            isFirst = false;
        }
            
        if (draggedItem.X + _itemWidth > width)
            return new Size(draggedItem.X + _itemWidth, height);
            
        return new Size(width, height);
    }

    private Size ArrangeImpl(Size finalSize)
    {
        double x = 0;
        var z = ZIndexes.NonSelected;
        var logicalIndex = 0;

        _itemsLocations.Clear();
            
        foreach (var child in Children)
        {
            if (child is not TabsViewItem tabItem)
                continue;

            tabItem.ZIndex = tabItem.IsSelected ? int.MaxValue : --z;
            tabItem.LogicalIndex = logicalIndex++;
            
            SetLocation(tabItem, x, _itemWidth);
                
            _itemsLocations.Add(tabItem, GetLocationInfo(tabItem));

            x += _itemWidth + ItemOffset;
        }

        return finalSize;
    }

    private Size DragArrangeImpl(TabsViewItem dragItem, Size finalSize)
    {
        var dragItemsLocations = GetLocations(Children.OfType<TabsViewItem>(), dragItem);
        
        var currentCoord = 0.0;

        foreach (var location in dragItemsLocations)
        {
            var item = location.Item;

            if (!Equals(item, dragItem) && item.LogicalIndex >= tabsControl.FixedHeaderCount)
            {
                SendToLocation(item, currentCoord, _itemWidth);
            }
            else
            {
                var maxX = finalSize.Width - _itemWidth;

                if (dragItem.X > maxX)
                    dragItem.X = maxX;

                var minX = CalculateMinX();

                if (dragItem.X < minX)
                    dragItem.X = minX;

                SetLocation(dragItem, dragItem.X, _itemWidth);
            }
                
            currentCoord += _itemWidth + ItemOffset;
        }
        
        return finalSize;
    }

    private double CalculateMinX()
    {
        if (tabsControl.FixedHeaderCount < 1)
            return 0;
        
        double x = 0;

        for (var index = 0; index < tabsControl.FixedHeaderCount; index++)
        {
            x += _itemWidth + ItemOffset;
        }

        return x;
    }

    private Size DragCompletedArrangeImpl(TabsViewItem dragItem, Size finalSize)
    {
        var dragItemsLocations = GetLocations(Children.OfType<TabsViewItem>(), dragItem);
            
        var currentCoord = 0.0;
        var z = ZIndexes.NonSelected;
        var logicalIndex = 0;

        foreach (var location in dragItemsLocations)
        {
            var item = location.Item;

            SetLocation(item, currentCoord, _itemWidth);
            currentCoord += _itemWidth + ItemOffset;
            item.ZIndex = --z;
            item.LogicalIndex = logicalIndex++;
        }

        dragItem.ZIndex = ZIndexes.Selected;
        
        DragCompleted?.Invoke();
            
        return finalSize;
    }

    private double GetAvailableWidth(Size availableSize)
    {
        var tabsCount = Children.Count;

        if (tabsCount == 0)
            return 0;

        var itemWidth = availableSize.Width / tabsCount - ItemOffset * (tabsCount - 1) / tabsCount;

        return double.Min(ItemWidth, itemWidth);
    }

    private IEnumerable<LocationInfo> GetLocations(IEnumerable<TabsViewItem> allItems, TabsViewItem dragItem)
    {
        double OrderSelector(LocationInfo loc)
        {
            if (Equals(loc.Item, dragItem))
            {
                if (!_itemsLocations.TryGetValue(dragItem, out var dragItemInfo))
                    return loc.Start;
                    
                return loc.Start > dragItemInfo.Start ? loc.End : loc.Start;
            }

            return _itemsLocations.TryGetValue(loc.Item, out var itemInfo) ? itemInfo.Mid : loc.Start;
        }

        var currentLocations = allItems
            .Select(GetLocationInfo)
            .OrderBy(OrderSelector);

        return currentLocations;
    }

    private async void SendToLocation(TabsViewItem item, double location, double width)
    {
        var itemIsAnimating = _activeStoryboardTargetLocations.TryGetValue(item, out var activeTarget);
        
        if (itemIsAnimating)
        {
            SetLocation(item, item.X, width);
            return;
        }
        
        if (double.Abs(item.X - location) < 1.0 || itemIsAnimating && double.Abs(activeTarget - location) < 1.0)
        {
            return;
        }
        
        _activeStoryboardTargetLocations[item] = location;

        const int animDuration = 200;

        var animation = new Animation
        {
            Easing = new CubicEaseOut(),
            Duration = TimeSpan.FromMilliseconds(animDuration),
            PlaybackDirection = PlaybackDirection.Normal,
            FillMode = FillMode.None,
            Children =
            {
                new KeyFrame
                {
                    KeyTime = TimeSpan.FromMilliseconds(animDuration),
                    Setters =
                    {
                        new Setter(TabsViewItem.XProperty, location)
                    }
                }
            }
        };
            
        await animation.RunAsync(item);

        SetLocation(item, location, width);
            
        _activeStoryboardTargetLocations.Remove(item);
    }

    private static void SetLocation(TabsViewItem dragTabItem, double x, double width)
    {
        const double y = 0;

        dragTabItem.X = x;
        dragTabItem.Y = y;

        dragTabItem.Arrange(new Rect(new Point(x, y), new Size(width, dragTabItem.DesiredSize.Height)));
    }

    private LocationInfo GetLocationInfo(TabsViewItem item)
    {
        var size = item.Bounds.Width;
            
        if (!_activeStoryboardTargetLocations.TryGetValue(item, out var startLocation))
            startLocation = item.X;
            
        var midLocation = startLocation + size / 2;
        var endLocation = startLocation + size;

        return new LocationInfo(item, startLocation, midLocation, endLocation);
    }

    private TabsViewItem? GetDragItem() => (TabsViewItem?)Children.FirstOrDefault(c => c is TabsViewItem
    {
        IsDragging: true
    });

    #region Private Structs

    private readonly record struct LocationInfo(TabsViewItem Item, double Start, double Mid, double End);

    #endregion
}
