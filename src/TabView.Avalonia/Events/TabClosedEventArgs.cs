using System;

namespace TabView.Avalonia;

public class TabClosedEventArgs(object? item, TabsViewItem tab) : EventArgs
{
    public object? Item { get; } = item;

    public TabsViewItem Tab { get; } = tab;
}
