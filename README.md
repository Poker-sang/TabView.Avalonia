# TabView.Avalonia

[![NuGet](https://img.shields.io/nuget/v/TabView.Avalonia?label=TabView.Avalonia&logo=nuget&style=for-the-badge)](https://www.nuget.org/packages/TabView.Avalonia)

A brand-new `TabsView` control that supports dragging and tearing, based on the `TabbedPage` control in [Avalonia](https://github.com/AvaloniaUI/Avalonia/) 12 and the [Tabalonia](https://github.com/egorozh/Tabalonia/) repository. AOT compatible.

![sample](./.github/imgs/sample.gif)

## Usage

Install the package from NuGet:

```pwsh
Install-Package TabView.Avalonia
# Or 'dotnet add package TabView.Avalonia'
```

Add the Style to your App.axaml:

```xml
<Application xmlns:tabView="using:TabView.Avalonia">
    ...
    <Application.Styles>
        <FluentTheme />
        <tabView:TabsViewFluentTheme />
    </Application.Styles>
</Application>
```

The original `TabsViewTheme` is now named `TabsViewFluentTheme`.
The two themes are independent: their entry points, resources and templates live in
`Themes/Fluent/` and `Themes/Fluent2/` respectively. Include one pair of themes.

For Fluent 2 / WinUI 3 styling, reference `Avalonia.Themes.Fluent2` and use:

```xml
<Application.Styles>
    <Fluent2Theme />
    <tabView:TabsViewFluent2Theme />
</Application.Styles>
```

This preview pins Avalonia to `12.2.999-cibuild0101180-alpha` in
`Directory.Build.props`, with the official nightly feed in `NuGet.Config`.
All Avalonia packages must use the same version. Fluent 2 comes from
[Avalonia PR #21771](https://github.com/AvaloniaUI/Avalonia/pull/21771).
The tab template follows the [WinUI 3 TabView XAML](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TabView/TabView.xaml),
its [resource dictionary](https://github.com/microsoft/microsoft-ui-xaml/blob/main/controls/dev/TabView/TabView_themeresources.xaml)
and selected-tab geometry in `TabViewItem.cpp`.

Run the demo with `dotnet run --project src/TabView.Avalonia.Demo`.
The tab-strip picker switches between Fluent, Fluent 2 and Material 3 while preserving pages,
selection and light/dark mode, including torn-off windows. The moon/sun button
switches light/dark mode. `-- --fluent1` starts with the original theme;
`-- --material3` starts with Material 3.

Fluent 2 dimensions and brushes use the `TabsViewFluent2` resource prefix, for
example `TabsViewFluent2ItemHeight`, `TabsViewFluent2ItemWidth`,
`TabsViewFluent2ItemCornerRadius` and `TabsViewFluent2ContentBackground`.
Override them in application resources; colors default to Fluent 2 semantic tokens
and follow the current theme variant. The tab strip currently uses the control's
horizontal layout and shrinks tabs to fit, as in the original theme.
The selected tab joins the content surface without WinUI's horizontal divider.
Add and close actions use vector paths with one-DIP strokes and no dependency on
system icon fonts or FluentIcons. `TabsViewFluent2ButtonGlyphSize` sets the plus size;
`TabsViewFluent2CloseButtonGlyphSize` gives the diagonal close mark a smaller optical size.
The plus uses integer path edges for crisp strokes at 100%.
Pixel regression tests cover the join in light/dark mode at 100%, 125%, 150% and 200%.
Run all checks with `dotnet test TabView.Avalonia.slnx`.

The Material 3 preview is isolated in `src/TabView.Avalonia.Material3/`, with its own
`Themes/Material3/` templates and a dependency on
[`Shirasagi0012.Material3Expressive.Avalonia` 0.1.0](https://github.com/Shirasagi0012/Material3Expressive.Avalonia).
The core library and Fluent themes do not depend on Material3Expressive or its Skia 4 stack.
Reference the Material 3 project and load its theme after `MaterialTheme`:

```xml
<Application
    xmlns="https://github.com/avaloniaui"
    xmlns:material="clr-namespace:Material3Expressive.Styles;assembly=Material3Expressive"
    xmlns:materialTabs="clr-namespace:TabView.Avalonia;assembly=TabView.Avalonia.Material3">
    <Application.Styles>
        <FluentTheme />
        <material:MaterialTheme />
        <materialTabs:TabsViewMaterial3Theme />
    </Application.Styles>
</Application>
```

Follow Material3Expressive's initialization: call `UseRoundedSymbols()` on the
AppBuilder and `ComposeAnimation.AnimationScope.Initialize(this)` before loading
each window's XAML. The Demo also uses the optional Roboto package and `WithRobotoFont()`.
Keep the managed/native Skia versions supplied by MaterialSymbols together.

Material document tabs follow Chrome's connected selected-tab shape: rounded top
corners, concave feet, shared selected/content color, quiet inactive separators,
and round close/add state layers. Hovering an inactive tab highlights its rounded
body and hides both adjacent dividers; the selected tab keeps its background.
The 20 DIP close feedback sits inside a 28 DIP hit target, while add feedback is 32 DIP.
Close buttons have a reserved area above 75 DIP tab width. At or below 75 DIP only
the selected tab keeps its close button, with the header yielding space to it.
`CloseButtonVisibilityThreshold` controls this boundary; fixed tabs and the global
close-button setting still apply. All three themes share `TabHeaderPanel` for
this rule, content clipping and close-button placement.
This is a document-tab adaptation rather than
the indicator-based navigation tabs in the Material specification. Compare
[Chromium's horizontal tab styling](https://github.com/chromium/chromium/blob/main/chrome/browser/ui/views/tabs/common/horizontal_tab_style_views.cc)
and Material3Expressive's [TabRow](https://github.com/Shirasagi0012/Material3Expressive.Avalonia/blob/main/src/Material3Expressive/Components/TabRow/README.md)
for the two purposes.

All Material tab colors, dimensions and typography resolve through `{m3:Token}`.
`TabsViewMaterial3TokenSource` maps the public `TabsViewMaterial3Tokens` key instances
to component defaults and `TokenRef` values. The default top radius references
`MaterialTokens.Sys.Shape.Medium` (12 DIP), with a 12 DIP foot. Selection and content
share `ContentColor`; light/dark variants resolve to Material surface roles.
Override these keys with an `ITokenSource` inside `MaterialResource`, at application
or control scope. System color/shape/type overrides and live palette changes resolve
through the references at the consuming control. Tests cover this resolution,
three-theme roundtrips, pointer feedback, and connected edges and circular feet
with 4, 12 and 28 tabs at 100%, 125%, 150% and 200% scaling.

And use the control just like `TabbedPage`:
```xml
<tabView:TabsView>
    <ContentPage Header="Tab 1" />
    <ContentPage Header="Tab 2" />
    <ContentPage Header="Tab 3" />
</tabView:TabsView>
```

But if you want to support AOT, you need to override the `TabsView.InterTabController` property and implement the `IInterTabClient` interface, like this:

```cs
MyTabsView.InterTabController = new InterTabController { InterTabClient = new AotInterTabClient() };

private sealed class AotInterTabClient : IInterTabClient
{
    public TabsHost GetNewHost(InterTabController controller, TabsHost host)
    {
        var window = new MainWindow { Owner = host.Window };
        return new TabsHost(window, window.MyTabsView);
    }
}
```

To disable tearing, you can set the `TabsView.InterTabController` property to `null`:

```cs
MyTabsView.InterTabController = null;
```
