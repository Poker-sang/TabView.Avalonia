namespace TabView.Avalonia;

public abstract class DragTabItemEventArgs(RoutedEvent routedEvent, TabsViewItem tabItem) : RoutedEventArgs(routedEvent)
{
    public TabsViewItem TabItem { get; } = tabItem;
}
