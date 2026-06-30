using System;

namespace TabView.Avalonia;

public class DragTabDragCompletedEventArgs(
    RoutedEvent routedEvent,
    TabsViewItem dragItem,
    VectorEventArgs dragCompletedEventArgs)
    : DragTabItemEventArgs(routedEvent, dragItem)
{
    public VectorEventArgs DragCompletedEventArgs { get; } = dragCompletedEventArgs ?? throw new ArgumentNullException(nameof(dragCompletedEventArgs));
}
