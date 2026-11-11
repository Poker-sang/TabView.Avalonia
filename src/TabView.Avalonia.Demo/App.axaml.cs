using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Fluent2;
using Avalonia.Styling;
using Material3Expressive.Styles;
using System.Linq;
using System.Collections.Generic;

namespace TabView.Avalonia.Demo;

public enum DemoTheme { Fluent, Fluent2, Material3 }

public partial class App : Application
{
    public static DemoTheme SelectedTheme { get; set; } = DemoTheme.Fluent2;
    public static bool UseFluent2
    {
        get => SelectedTheme == DemoTheme.Fluent2;
        set => SelectedTheme = value ? DemoTheme.Fluent2 : DemoTheme.Fluent;
    }
    public static string ThemeName => SelectedTheme switch
    {
        DemoTheme.Material3 => "Material 3",
        DemoTheme.Fluent2 => "Fluent 2",
        _ => "Fluent"
    };
    private readonly HashSet<MainWindow> _windows = [];

    internal void RegisterWindow(MainWindow window) => _windows.Add(window);
    internal void UnregisterWindow(MainWindow window) => _windows.Remove(window);

    public void SetFluentTheme(bool fluent2) => SetTheme(fluent2 ? DemoTheme.Fluent2 : DemoTheme.Fluent);

    public void SetTheme(DemoTheme theme)
    {
        if (SelectedTheme == theme)
            return;

        var windows = _windows.Select(window => (Window: window, State: window.DetachTabs())).ToArray();

        SelectedTheme = theme;
        ApplyTheme();

        foreach (var window in windows)
            window.Window.RestoreTabs(window.State);
    }

    private void ApplyTheme()
    {
        Styles[0] = SelectedTheme switch
        {
            DemoTheme.Material3 => new Styles { new FluentTheme(), new MaterialTheme() },
            DemoTheme.Fluent2 => new Fluent2Theme(),
            _ => new FluentTheme()
        };
        Styles[1] = SelectedTheme switch
        {
            DemoTheme.Material3 => new Styles { new TabsViewMaterial3Theme(), new Material3DemoStyles() },
            DemoTheme.Fluent2 => new TabsViewFluent2Theme(),
            _ => new TabsViewFluentTheme()
        };
        Resources.MergedDictionaries[0] = SelectedTheme == DemoTheme.Material3
            ? new Material3DemoResources()
            : new FluentDemoResources();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        if (SelectedTheme != DemoTheme.Fluent2)
            ApplyTheme();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = new MainWindow();
            desktop.MainWindow = mainWindow;
            mainWindow.AddFirstPage();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
