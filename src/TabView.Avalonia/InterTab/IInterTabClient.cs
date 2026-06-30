namespace TabView.Avalonia;

/// <summary>
/// Provides the strategy for creating new windows when tabs are torn off,
/// and for handling scenarios where all tabs have been removed from a window.
/// </summary>
public interface IInterTabClient
{
    /// <summary>
    /// Called when a tab is torn off and a new host window is needed.
    /// The returned window should contain a <see cref="TabsView"/> ready to receive the dragged tab.
    /// The window should NOT be shown yet — the caller will position and show it.
    /// </summary>
    TabsHost GetNewHost(InterTabController controller, TabsHost host);
}
