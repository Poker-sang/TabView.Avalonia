using System;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace TabView.Avalonia;

/// <summary>
/// Chrome inspired document tabs using Material3Expressive color and interaction tokens.
/// Add after MaterialTheme in Application.Styles. Initialize an AnimationScope on each window.
/// </summary>
public class TabsViewMaterial3Theme : Styles
{
    /// <summary>Creates the Material 3 tab themes.</summary>
    public TabsViewMaterial3Theme(IServiceProvider? sp = null) => AvaloniaXamlLoader.Load(sp, this);
}
