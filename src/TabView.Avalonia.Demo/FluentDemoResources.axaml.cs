using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabView.Avalonia.Demo;

public partial class FluentDemoResources : ResourceDictionary
{
    public FluentDemoResources() => AvaloniaXamlLoader.Load(this);
}
