using Avalonia.Controls;
using Avalonia.Interactivity;

namespace TabView.Avalonia.Demo;

public partial class ShowcasePage : UserControl
{
    public ShowcasePage()
    {
        InitializeComponent();
        RefreshThemeLabel();
    }

    internal void RefreshThemeLabel() => ThemeLabel.Text = App.ThemeName.ToUpperInvariant();

    public ShowcasePage(string eyebrow, string heading, string description) : this()
    {
        Eyebrow.Text = eyebrow;
        Heading.Text = heading;
        Description.Text = description;
    }

    private void NewTab_OnClick(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is MainWindow window)
            window.MyTabsView.AddItemCommand.Execute(null);
    }
}
