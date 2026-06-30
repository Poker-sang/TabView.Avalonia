using System.ComponentModel;

namespace TabView.Avalonia;

public class TabClosingEventArgs(object? item, TabsViewItem tab) : CancelEventArgs
{
    public object? Item { get; } = item;

    public TabsViewItem Tab { get; } = tab;
}
