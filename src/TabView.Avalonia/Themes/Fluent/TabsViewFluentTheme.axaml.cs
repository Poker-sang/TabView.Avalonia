using System;
using Avalonia.Markup.Xaml;

namespace TabView.Avalonia;

/// <summary>
/// The original Fluent theme for TabsView. Add this after FluentTheme in Application.Styles.
/// </summary>
public class TabsViewFluentTheme : Styles
{
    /// <summary>Creates the Fluent styles and resources.</summary>
    public TabsViewFluentTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
