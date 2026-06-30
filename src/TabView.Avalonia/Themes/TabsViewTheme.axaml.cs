using System;
using Avalonia.Markup.Xaml;

namespace TabView.Avalonia;

public class TabsViewTheme : Styles
{
    public TabsViewTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}