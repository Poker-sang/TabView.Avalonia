using System;
using Avalonia.Markup.Xaml;

namespace TabView.Avalonia;

/// <summary>
/// WinUI 3 style tabs for applications using Avalonia.Themes.Fluent2.Fluent2Theme.
/// Add this after Fluent2Theme in Application.Styles.
/// </summary>
public class TabsViewFluent2Theme : Styles
{
    /// <summary>Creates the Fluent 2 styles and resources.</summary>
    public TabsViewFluent2Theme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
