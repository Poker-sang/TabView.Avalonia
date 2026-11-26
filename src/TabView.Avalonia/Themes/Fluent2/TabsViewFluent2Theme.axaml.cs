using System;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

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
        // A brush override supplies a shared fill for the selected tab and page.
        Resources["TabsViewFluent2ContentBackground"] = null;
    }
}

internal static class Fluent2TabBackground
{
    public static IMultiValueConverter SelectBrush { get; } =
        new FuncMultiValueConverter<object?, IBrush?>(values => values.OfType<IBrush>().FirstOrDefault());
}
