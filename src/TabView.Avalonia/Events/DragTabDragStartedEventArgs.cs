namespace TabView.Avalonia;

public class DragTabDragStartedEventArgs(
    RoutedEvent routedEvent,
    TabsViewItem tabItem,
    VectorEventArgs dragStartedEventArgs)
    : DragTabItemEventArgs(routedEvent, tabItem)
{
    public VectorEventArgs DragStartedEventArgs { get; } = dragStartedEventArgs;
}
