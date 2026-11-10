using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Themes.Fluent2;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;
using MaterialSymbols.Avalonia;
using MaterialTypography.Roboto;

[assembly: AvaloniaTestApplication(typeof(TabView.Avalonia.Tests.TestAppBuilder))]

namespace TabView.Avalonia.Tests;

public static class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<Demo.App>()
        .UseSkia()
        .WithRobotoFont()
        .UseRoundedSymbols()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

public class ThemeTests
{
    [AvaloniaTheory]
    [InlineData(900, 32)]
    [InlineData(400, 32)]
    [InlineData(900, 48)]
    [InlineData(400, 48)]
    public void Fluent2AddButtonHonorsBottomAlignment(double width, double headerHeight)
    {
        var (window, tabs) = OpenTabs(true);
        try
        {
            window.Width = width;
            tabs.RightHeaderContent = new Border { Height = headerHeight };
            Flush(window);
            var add = tabs.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "PART_AddItemButton");
            var strip = Assert.IsType<Primitives.TopPanel>(add.GetVisualParent());
            Assert.Equal(strip.Bounds.Height - add.Margin.Bottom, add.Bounds.Bottom);
            Assert.Equal(add.Height, add.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Fluent2PlusHasCrispSinglePixelStrokesAt100Percent()
    {
        var (window, tabs) = OpenTabs(true);
        try
        {
            var add = tabs.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "PART_AddItemButton");
            add.Foreground = Brushes.White;
            add.Background = Brushes.Black;
            Flush(window);
            var size = new PixelSize((int)add.Bounds.Width, (int)add.Bounds.Height);
            using var bitmap = new RenderTargetBitmap(size, new Vector(96, 96));
            bitmap.Render(add);
            var bytes = new byte[size.Width * size.Height * 4];
            var pixels = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                bitmap.CopyPixels(new PixelRect(size), pixels, bytes.Length, size.Width * 4);
                Marshal.Copy(pixels, bytes, 0, bytes.Length);
                var ink = 0;
                for (var offset = 0; offset < bytes.Length; offset += 4)
                {
                    Assert.True(bytes[offset] is 0 or 255, "A plus edge was antialiased across pixels.");
                    if (bytes[offset] == 255)
                        ink++;
                }
                var glyph = add.GetVisualDescendants().OfType<PathIcon>().Single();
                Assert.Equal((int)(glyph.Bounds.Width + glyph.Bounds.Height - 1), ink);
            }
            finally { Marshal.FreeHGlobal(pixels); }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1, false)]
    [InlineData(1.25, false)]
    [InlineData(1.5, false)]
    [InlineData(2, false)]
    [InlineData(1, true)]
    [InlineData(1.25, true)]
    [InlineData(1.5, true)]
    [InlineData(2, true)]
    public void Fluent2HasNoPixelSeamBetweenSelectedTabAndContent(double scale, bool dark)
    {
        var (window, tabs) = OpenTabs(true);
        try
        {
            window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            Flush(window);
            var selected = tabs.SelectedTabItem!;
            var selectedOrigin = selected.TranslatePoint(default, tabs)!.Value;
            var content = tabs.GetVisualDescendants().OfType<Panel>()
                .Single(panel => panel.Name == "PART_ContentRoot");
            var contentOrigin = content.TranslatePoint(default, tabs)!.Value;
            var expected = Assert.IsAssignableFrom<ISolidColorBrush>(content.Background).Color;
            using var bitmap = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(tabs.Bounds.Width * scale), (int)Math.Ceiling(tabs.Bounds.Height * scale)),
                new Vector(96 * scale, 96 * scale));
            bitmap.Render(tabs);
            var pixel = Marshal.AllocHGlobal(4);
            try
            {
                foreach (var x in new[] { selectedOrigin.X + 16, selectedOrigin.X + selected.Bounds.Width / 2,
                             selectedOrigin.X + selected.Bounds.Width - 16 })
                {
                    var joinY = (int)Math.Round(contentOrigin.Y * scale);
                    for (var y = joinY - 2; y <= joinY + 2; y++)
                    {
                        bitmap.CopyPixels(new PixelRect((int)Math.Round(x * scale), y, 1, 1), pixel, 4, 4);
                        var bytes = new byte[4];
                        Marshal.Copy(pixel, bytes, 0, bytes.Length);
                        Assert.Equal(new[] { expected.B, expected.G, expected.R, expected.A }, bytes);
                    }
                }
            }
            finally { Marshal.FreeHGlobal(pixel); }
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DemoCanSwitchBackAndForthWithOpenPickerAndRetainBothWindows()
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        Demo.App.UseFluent2 = true;
        app.Styles.Clear();
        app.Styles.Add(new Fluent2Theme());
        app.Styles.Add(new TabsViewFluent2Theme());
        app.RequestedThemeVariant = ThemeVariant.Dark;
        var first = new Demo.MainWindow();
        first.AddFirstPage();
        var pages = Pages(first.MyTabsView);
        var editor = new TextBox { Text = "Keep this edit" };
        ((ContentPage)pages[0]).Content = editor;
        var movedPage = pages[2];
        pages.Remove(movedPage);
        first.MyTabsView.SelectedIndex = 1;
        var selectedPage = pages[1];
        var controller = first.MyTabsView.InterTabController!;
        var host = controller.InterTabClient.GetNewHost(controller, new TabsHost(first, first.MyTabsView));
        var second = Assert.IsType<Demo.MainWindow>(host.Window);
        Pages(second.MyTabsView).Add(movedPage);
        first.Show();
        second.Show();
        Flush(first);
        Flush(second);
        try
        {
            for (var i = 0; i < 8; i++)
            {
                var picker = first.GetVisualDescendants().OfType<ComboBox>().Single();
                picker.IsDropDownOpen = true;
                Flush(first);
                for (var index = 0; index < 2; index++)
                {
                    var item = Assert.IsType<ComboBoxItem>(picker.ContainerFromIndex(index));
                    Assert.True(item.Bounds.Height > 0);
                    Assert.Contains(item.GetVisualDescendants().OfType<TextBlock>(),
                        text => text.Text == (index == 0 ? "Fluent" : "Fluent 2"));
                }
                var fluent2 = i % 2 != 0;
                picker.SelectedIndex = fluent2 ? 1 : 0;
                Flush(first);
                Flush(second);

                Assert.Equal(fluent2, Demo.App.UseFluent2);
                Assert.Equal(2, Pages(first.MyTabsView).Count);
                Assert.Same(selectedPage, first.MyTabsView.SelectedPage);
                Assert.Same(editor, ((ContentPage)Pages(first.MyTabsView)[0]).Content);
                Assert.Equal("Keep this edit", editor.Text);
                Assert.Same(movedPage, Assert.Single(Pages(second.MyTabsView)));
                Assert.Equal(ThemeVariant.Dark, app.RequestedThemeVariant);
                Assert.Equal(fluent2 ? 1 : 0, second.GetVisualDescendants().OfType<ComboBox>().Single().SelectedIndex);
            }
        }
        finally
        {
            second.Close();
            first.Close();
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ThemesLoadIndependentlyAndFollowLightDarkVariants(bool fluent2)
    {
        var (window, tabs) = OpenTabs(fluent2);
        try
        {
            var selected = tabs.SelectedTabItem!;
            Assert.NotNull(selected);
            Assert.Equal(32, selected.Bounds.Height);
            var light = selected.Background!.ToString();

            window.RequestedThemeVariant = ThemeVariant.Dark;
            Flush(window);
            Assert.NotEqual(light, selected.Background!.ToString());
            Assert.NotNull(selected.Foreground);
            Assert.Equal("Content 0", tabs.GetVisualDescendants().OfType<TextBlock>()
                .Single(text => text.Text == "Content 0").Text);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void CloseAndAddBindingsRespectFixedTabsAndVisibility(bool fluent2)
    {
        var (window, tabs) = OpenTabs(fluent2);
        try
        {
            tabs.FixedHeaderCount = 1;
            Flush(window);
            var items = Items(tabs);
            Assert.False(CloseButton(items[0]).IsVisible);
            Assert.True(CloseButton(items[1]).IsVisible);

            var add = tabs.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Name == "PART_AddItemButton");
            var invoked = false;
            tabs.AddTabButtonClick += (_, _) => invoked = true;
            add.Command!.Execute(add.CommandParameter);
            Assert.True(invoked);

            tabs.SelectedIndex = 1;
            Flush(window);
            var close = CloseButton(items[1]);
            close.Command!.Execute(close.CommandParameter);
            Flush(window);
            Assert.Equal(2, Pages(tabs).Count);
            Assert.DoesNotContain(Pages(tabs), page => Equals(page.Header, "Tab 1"));

            tabs.ShowDefaultCloseButton = false;
            tabs.ShowDefaultAddButton = false;
            Flush(window);
            Assert.All(Items(tabs), item => Assert.False(CloseButton(item).IsVisible));
            Assert.False(add.IsVisible);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Fluent2HidesDividersOnBothSidesOfSelectedTab()
    {
        var (window, tabs) = OpenTabs(true);
        try
        {
            var items = Items(tabs);
            Assert.False(Separator(items[0]).IsVisible);
            Assert.True(Separator(items[1]).IsVisible);
            Assert.False(Separator(items[2]).IsVisible);

            tabs.SelectedIndex = 1;
            Flush(window);
            Assert.All(items, item => Assert.False(Separator(item).IsVisible));
            Assert.Equal(FontWeight.SemiBold, items[1].FontWeight);
            Assert.Equal(FontWeight.Normal, items[0].FontWeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Fluent2ScalesTemplatedIconsAndRemovesEmptyIconSpace()
    {
        var tabs = new TabsView { InterTabController = null, RightIndent = 0 };
        var page = new ContentPage
        {
            Header = "An icon template",
            Icon = "icon data",
            IconTemplate = new FuncDataTemplate<string>((_, _) =>
                new Border { Width = 64, Height = 32, Background = Brushes.Blue }),
            Content = new TextBlock { Text = "Page content" }
        };
        Pages(tabs).Add(page);
        var window = CreateWindow(true, tabs);
        try
        {
            window.Show();
            Flush(window);
            var iconBox = tabs.SelectedTabItem!.GetVisualDescendants().OfType<Viewbox>()
                .Single(box => box.Name == "PART_IconBox");
            // A 2:1 icon must stay 2:1 inside the 16px slot.
            Assert.Equal(new Size(16, 8), iconBox.Bounds.Size);
            var presenter = Assert.IsType<ContentPresenter>(iconBox.Child);
            Assert.Equal("icon data", presenter.Content);
            Assert.Same(page.IconTemplate, presenter.ContentTemplate);
            Assert.IsType<Border>(presenter.Child);

            page.Icon = null;
            Flush(window);
            Assert.False(iconBox.IsVisible);
        }
        finally { window.Close(); }
    }

    private static (Window Window, TabsView Tabs) OpenTabs(bool fluent2)
    {
        var tabs = new TabsView { InterTabController = null, RightIndent = 0 };
        for (var i = 0; i < 3; i++)
            Pages(tabs).Add(new ContentPage
            {
                Header = $"Tab {i}",
                Content = new TextBlock { Text = $"Content {i}" }
            });
        tabs.SelectedIndex = 0;
        var window = CreateWindow(fluent2, tabs);
        window.Show();
        Flush(window);
        return (window, tabs);
    }

    private static Window CreateWindow(bool fluent2, TabsView tabs)
    {
        var styles = Application.Current!.Styles;
        styles.Clear();
        styles.Add(fluent2 ? new Fluent2Theme() : new FluentTheme());
        styles.Add(fluent2 ? new TabsViewFluent2Theme() : new TabsViewFluentTheme());
        var window = new Window
        {
            Width = 900, Height = 400, Content = tabs,
            RequestedThemeVariant = ThemeVariant.Light
        };
        return window;
    }

    private static TabsViewItem[] Items(TabsView tabs) => tabs.GetVisualDescendants()
        .OfType<TabsViewItem>().OrderBy(item => item.LogicalIndex).ToArray();

    private static IList<Page> Pages(TabsView tabs) => Assert.IsAssignableFrom<IList<Page>>(tabs.Pages);

    private static Button CloseButton(TabsViewItem item) => item.GetVisualDescendants()
        .OfType<Button>().Single();

    private static Border Separator(TabsViewItem item) => item.GetVisualDescendants()
        .OfType<Border>().Single(border => border.Name == "PART_Separator");

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
