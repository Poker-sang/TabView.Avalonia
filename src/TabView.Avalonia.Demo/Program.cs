using Avalonia;
using System;
using System.Linq;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

namespace TabView.Avalonia.Demo;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        App.SelectedTheme = args.Contains("--material3") ? DemoTheme.Material3 :
            args.Contains("--fluent1") ? DemoTheme.Fluent : DemoTheme.Fluent2;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .WithRobotoFont()
            .UseRoundedSymbols()
            .LogToTrace();
}
