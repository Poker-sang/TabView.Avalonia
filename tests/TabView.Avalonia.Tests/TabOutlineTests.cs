using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Xunit;

namespace TabView.Avalonia.Tests;

public class TabOutlineTests
{
    [AvaloniaTheory]
    [InlineData(Demo.DemoTheme.Fluent, 4)]
    [InlineData(Demo.DemoTheme.Fluent, 12)]
    [InlineData(Demo.DemoTheme.Fluent, 16)]
    [InlineData(Demo.DemoTheme.Fluent, 28)]
    [InlineData(Demo.DemoTheme.Fluent2, 4)]
    [InlineData(Demo.DemoTheme.Fluent2, 12)]
    [InlineData(Demo.DemoTheme.Fluent2, 16)]
    [InlineData(Demo.DemoTheme.Fluent2, 28)]
    public void CrowdedFluentTabsKeepTheActiveCloseButtonAndRestoreOthersWhenSpaceReturns(Demo.DemoTheme theme, int count)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(theme == Demo.DemoTheme.Fluent2 ? Demo.DemoTheme.Fluent : Demo.DemoTheme.Fluent2);
        app.SetTheme(theme);
        var tabs = new TabsView { InterTabController = null, RightIndent = 0 };
        var pages = (IList<Page>)tabs.Pages;
        for (var i = 0; i < count; i++)
            pages.Add(new ContentPage
            {
                Header = $"Document {i}",
                Icon = new Border { Width = 16, Height = 16, Background = Brushes.Blue }
            });
        tabs.SelectedIndex = 1;
        var window = new Window { Width = 840, Height = 180, Content = tabs };
        window.Show();
        Flush(window);
        try
        {
            var items = Items();
            Assert.All(items, item => Assert.Equal(item.IsSelected || item.Bounds.Width > 75, Close(item).IsVisible));
            AssertContainedButtons(items);

            window.Width = Math.Max(840, count * 100 + 40);
            Flush(window);
            Assert.All(items, item => Assert.True(Close(item).IsVisible));
            AssertContainedButtons(items);

            // Exercise hit testing after the button has been hidden and revealed.
            var target = pages[1];
            var close = Close(items[1]);
            var point = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            window.MouseMove(point);
            Flush(window);
            Assert.True(close.IsPointerOver);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Flush(window);
            Assert.Equal(count - 1, pages.Count);
            Assert.DoesNotContain(target, pages);

            TabsViewItem[] Items() => tabs.GetVisualDescendants().OfType<TabsViewItem>()
                .OrderBy(item => item.LogicalIndex).ToArray();

            static Button Close(TabsViewItem item) => item.GetVisualDescendants().OfType<Button>().Single();

            static void AssertContainedButtons(TabsViewItem[] containers)
            {
                foreach (var item in containers)
                {
                    var button = Close(item);
                    if (!button.IsVisible)
                        continue;
                    var origin = button.TranslatePoint(default, item)!.Value;
                    Assert.True(origin.X >= 0 && origin.X + button.Bounds.Width <= item.Bounds.Width,
                        "A close button extends beyond its own tab.");
                }
            }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Demo.DemoTheme.Fluent)]
    [InlineData(Demo.DemoTheme.Fluent2)]
    public void FluentCloseButtonsPrioritizeCompleteGlyphsAtTheCompactBoundary(Demo.DemoTheme theme)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(theme == Demo.DemoTheme.Fluent2 ? Demo.DemoTheme.Fluent : Demo.DemoTheme.Fluent2);
        app.SetTheme(theme);
        var tabs = new TabsView
        {
            InterTabController = null, RightIndent = 0, ShowDefaultAddButton = false, TabItemWidth = 76
        };
        var pages = (IList<Page>)tabs.Pages;
        for (var index = 0; index < 3; index++)
            pages.Add(new ContentPage
            {
                Header = $"A long document title {index}",
                Icon = new Border { Width = 64, Height = 64, Background = Brushes.Blue }
            });
        tabs.SelectedIndex = 1;
        var window = new Window { Width = 600, Height = 120, Content = tabs };
        window.Show();
        Flush(window);
        try
        {
            var items = tabs.GetVisualDescendants().OfType<TabsViewItem>().OrderBy(t => t.LogicalIndex).ToArray();
            Check(76);
            foreach (var item in items)
            {
                item.Padding = new Thickness(40, 0);
                var icon = item.GetVisualDescendants().OfType<Viewbox>().Single(v => v.Name == "PART_IconBox");
                icon.Width = icon.Height = 48;
            }
            Flush(window);
            Check(76); // The close target takes priority over oversized content and padding.
            foreach (var width in new[] { 75d, 60, 30, 20 })
            {
                tabs.TabItemWidth = width;
                Flush(window);
                Check(width);
                tabs.SelectedIndex = (tabs.SelectedIndex + 1) % 3;
                Flush(window);
                Check(width);
            }
            tabs.TabItemWidth = 76;
            Flush(window);
            Check(76);
            tabs.Resources["TabsViewCompactWidthThreshold"] = 80d;
            Flush(window);
            Check(76, 80);
            tabs.Resources.Remove("TabsViewCompactWidthThreshold");
            Flush(window);
            Check(76);
            tabs.FixedHeaderCount = 1;
            tabs.SelectedIndex = 0;
            tabs.TabItemWidth = 30;
            Flush(window);
            Check(30);
            tabs.SelectedIndex = 1;
            Flush(window);
            Check(30);
            tabs.ShowDefaultCloseButton = false;
            Flush(window);
            Check(30);
            tabs.ShowDefaultCloseButton = true;
            Flush(window);
            Check(30);

            var target = tabs.SelectedPage;
            var close = Close(items[1]);
            var click = close.TranslatePoint(new Point(close.Bounds.Width / 2, close.Bounds.Height / 2), window)!.Value;
            window.MouseMove(click);
            Flush(window);
            Assert.True(close.IsPointerOver);
            window.MouseDown(click, MouseButton.Left);
            window.MouseUp(click, MouseButton.Left);
            Flush(window);
            Assert.Equal(2, pages.Count);
            Assert.DoesNotContain(target, pages);

            void Check(double width, double threshold = 75)
            {
                foreach (var item in items)
                {
                    Assert.Equal(width, item.Bounds.Width);
                    var button = Close(item);
                    var visible = tabs.ShowDefaultCloseButton && item.LogicalIndex >= tabs.FixedHeaderCount &&
                        (item.IsSelected || width > threshold);
                    Assert.Equal(visible, button.IsVisible);
                    if (!visible) continue;
                    var origin = button.TranslatePoint(default, item)!.Value;
                    Assert.True(origin.X >= 0 && origin.X + button.Bounds.Width <= width,
                        "The complete close target must stay inside its tab.");
                    if (width > 75)
                        Assert.Equal(theme == Demo.DemoTheme.Fluent ? 16d : 32d, button.Bounds.Width);
                    Control glyph = theme == Demo.DemoTheme.Fluent
                        ? button.GetVisualDescendants().OfType<Viewbox>().Single()
                        : button.GetVisualDescendants().OfType<PathIcon>().Single();
                    var glyphOrigin = glyph.TranslatePoint(default, button)!.Value;
                    Assert.True(glyphOrigin.X >= 0 && glyphOrigin.X + glyph.Bounds.Width <= button.Bounds.Width &&
                        glyphOrigin.Y >= 0 && glyphOrigin.Y + glyph.Bounds.Height <= button.Bounds.Height,
                        "The close glyph must remain whole even when content cannot fit.");
                }
            }
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }

        static Button Close(TabsViewItem item) => item.GetVisualDescendants().OfType<Button>()
            .Single(b => b.Name == "PART_CloseButton");
    }

    [AvaloniaTheory]
    [InlineData(900, 1)]
    [InlineData(900, 1.25)]
    [InlineData(900, 1.5)]
    [InlineData(900, 2)]
    [InlineData(450, 1)]
    [InlineData(450, 1.25)]
    [InlineData(450, 1.5)]
    [InlineData(450, 2)]
    public void SelectedFluent2TabHasCurvedFeetOutsideBothEdges(double width, double scale)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Fluent);
        app.SetTheme(Demo.DemoTheme.Fluent2);
        var tabs = new TabsView { InterTabController = null, RightIndent = 0 };
        // High contrast makes clipped feet visible to the pixel check in either palette.
        tabs.Resources["TabsViewFluent2StripBackground"] = Brushes.Black;
        tabs.Resources["TabsViewFluent2ContentBackground"] = Brushes.White;
        for (var i = 0; i < 3; i++)
            ((IList<Page>)tabs.Pages).Add(new ContentPage { Header = $"Tab {i}" });
        tabs.SelectedIndex = 1;
        var window = new Window { Width = width, Height = 100, Content = tabs, RequestedThemeVariant = ThemeVariant.Light };
        window.Show();
        Flush(window);
        try
        {
            var selected = tabs.SelectedTabItem!;
            var origin = selected.TranslatePoint(default, tabs)!.Value;
            var content = tabs.GetVisualDescendants().OfType<Panel>().Single(c => c.Name == "PART_ContentRoot");
            var join = content.TranslatePoint(default, tabs)!.Value;
            using var bitmap = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(width * scale), (int)Math.Ceiling(100 * scale)),
                new Vector(96 * scale, 96 * scale));
            bitmap.Render(tabs);
            var pixel = Marshal.AllocHGlobal(4);
            try
            {
                var bottom = (int)Math.Round(join.Y * scale) - 1;
                var aboveFoot = (int)Math.Floor((join.Y - 5) * scale);
                var left = (int)Math.Floor(origin.X * scale) - 1;
                var right = (int)Math.Ceiling((origin.X + selected.Bounds.Width) * scale);
                foreach (var x in new[] { left, right })
                {
                    Assert.True(ReadBlue(x, bottom) > 0, "The tab's outward foot was clipped.");
                    Assert.Equal(0, ReadBlue(x, aboveFoot));
                }
                // A rectangular border must not continue down through the curved join.
                Assert.Equal(255, ReadBlue((int)Math.Ceiling(origin.X * scale), bottom));
                Assert.Equal(255, ReadBlue((int)Math.Floor((origin.X + selected.Bounds.Width) * scale) - 1, bottom));

                byte ReadBlue(int x, int y)
                {
                    bitmap.CopyPixels(new PixelRect(x, y, 1, 1), pixel, 4, 4);
                    return Marshal.ReadByte(pixel);
                }
            }
            finally { Marshal.FreeHGlobal(pixel); }
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Demo.DemoTheme.Fluent)]
    [InlineData(Demo.DemoTheme.Fluent2)]
    [InlineData(Demo.DemoTheme.Material3)]
    public void HoverHidesBothAdjacentDividersAndRestoresThemOnExit(Demo.DemoTheme theme)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(theme == Demo.DemoTheme.Fluent2 ? Demo.DemoTheme.Fluent : Demo.DemoTheme.Fluent2);
        app.SetTheme(theme);
        var window = new Demo.MainWindow();
        window.AddFirstPage();
        ((IList<Page>)window.MyTabsView.Pages).Add(new ContentPage { Header = "Fourth tab" });
        window.MyTabsView.SelectedIndex = 0;
        window.Show();
        Flush(window);
        try
        {
            var items = window.MyTabsView.GetVisualDescendants().OfType<TabsViewItem>()
                .OrderBy(item => item.LogicalIndex).ToArray();
            var left = Divider(items[1]);
            var right = Divider(items[2]);
            Assert.True(left.IsVisible);
            Assert.True(right.IsVisible);

            window.MouseMove(items[2].TranslatePoint(new Point(items[2].Bounds.Width / 2, items[2].Bounds.Height / 2), window)!.Value);
            Flush(window);
            Assert.True(items[2].IsPointerOver);
            Assert.False(left.IsVisible);
            Assert.False(right.IsVisible);

            window.MouseMove(new Point(10, 100));
            Flush(window);
            Assert.False(items[2].IsPointerOver);
            Assert.True(left.IsVisible);
            Assert.True(right.IsVisible);
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
    }

    private static Border Divider(TabsViewItem item) => item.GetVisualDescendants().OfType<Border>()
        .Single(border => border.Name is "PART_Separator" or "PART_RightBorder");

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
