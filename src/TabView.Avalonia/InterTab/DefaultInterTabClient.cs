using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TabView.Avalonia;

/// <summary>
/// Default implementation of <see cref="IInterTabClient"/>.
/// Creates a new window of the same type as the source window using reflection,
/// and finds the first <see cref="TabsView"/> in its logical tree.
/// </summary>
public sealed class DefaultInterTabClient : IInterTabClient
{
    public TabsHost GetNewHost(
        InterTabController controller,
        TabsHost host)
    {
        Window newWindow;
        if (RuntimeFeature.IsDynamicCodeSupported)
#pragma warning disable IL2072
            newWindow = (Window)Activator.CreateInstance(host.Window.GetType())!;
#pragma warning restore IL2072
        else
            throw new InvalidOperationException(
                $"Dynamic code generation is not supported now, so {nameof(DefaultInterTabClient)} cannot create a new {nameof(Window)}. Please implement a custom {nameof(IInterTabClient)} that creates a new window using platform-specific APIs.");

        var tabsControl =
            newWindow.GetLogicalDescendants()
                .OfType<TabsView>()
                .FirstOrDefault()
            ?? throw new InvalidOperationException(
                $"New {nameof(Window)} of type '{host.Window.GetType().Name}' must contain a {nameof(TabView)}.");

        return new TabsHost(newWindow, tabsControl);
    }
}
