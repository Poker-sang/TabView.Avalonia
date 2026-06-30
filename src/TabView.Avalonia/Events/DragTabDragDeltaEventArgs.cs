using System;

namespace TabView.Avalonia;

public class DragTabDragDeltaEventArgs(RoutedEvent routedEvent, TabsViewItem tabItem, VectorEventArgs dragDeltaEventArgs)
    : DragTabItemEventArgs(routedEvent, tabItem)
{
    public VectorEventArgs DragDeltaEventArgs { get; } = dragDeltaEventArgs ?? throw new ArgumentNullException(nameof(dragDeltaEventArgs));

    /// <summary>
    /// The pointer's position in screen coordinates at the time of this drag delta.
    /// </summary>
    public PixelPoint PointerScreenPosition { get; init; }

    public bool Cancel { get; set; }        
}
