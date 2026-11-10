using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using FluentIcons.Common;

namespace TabView.Avalonia.Demo;

public partial class ThemeControls : UserControl
{
    public ThemeControls()
    {
        InitializeComponent();
        ThemePicker.SelectedIndex = (int)App.SelectedTheme;
        RefreshAppearanceIcon();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (Application.Current is { } app)
            app.PropertyChanged += Application_OnPropertyChanged;
        RefreshAppearanceIcon();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (Application.Current is { } app)
            app.PropertyChanged -= Application_OnPropertyChanged;
        base.OnDetachedFromVisualTree(e);
    }

    private void Application_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Application.ActualThemeVariantProperty)
            RefreshAppearanceIcon();
    }

    private void RefreshAppearanceIcon() => ThemeIcon.Symbol =
        Application.Current?.ActualThemeVariant == ThemeVariant.Dark ? Symbol.WeatherSunny : Symbol.WeatherMoon;

    private void ThemePicker_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ThemePicker.SelectedIndex < 0 || Application.Current is not App app)
            return;
        var theme = (DemoTheme)ThemePicker.SelectedIndex;
        if (theme == App.SelectedTheme)
            return;

        // Finish the ComboBox selection before replacing the theme and its popup.
        ThemePicker.IsDropDownOpen = false;
        Dispatcher.UIThread.Post(() => app.SetTheme(theme));
    }

    private void ToggleTheme_OnClick(object? sender, RoutedEventArgs e)
    {
        if (Application.Current is { } app)
            app.RequestedThemeVariant = app.ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }
}
