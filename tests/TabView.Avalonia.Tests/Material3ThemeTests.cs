using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Baseline;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using ComposeAnimation;
using ComposeAnimation.TestSupport;
using Xunit;

namespace TabView.Avalonia.Tests;

public class Material3ThemeTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaterialCloseButtonsRespectWidthSelectionAndStayInsideTheirTabs(bool withIcons)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        var tabs = new TabsView { InterTabController = null, RightIndent = 0, ShowDefaultAddButton = false };
        for (var index = 0; index < 10; index++)
            ((IList<Page>)tabs.Pages).Add(new ContentPage
            {
                Header = $"Document {index}",
                Icon = withIcons ? new Border { Width = 16, Height = 16, Background = Brushes.Red } : null
            });
        var window = new Window { Width = 760, Height = 100 };
        AnimationScope.Initialize(window);
        window.Content = tabs;
        window.Show();
        Flush(window);
        try
        {
            CheckButtons(true);
            window.Width = 750;
            Flush(window);
            CheckButtons(null);
            window.Width = 600;
            Flush(window);
            CheckButtons(null);
            window.Width = 1200;
            Flush(window);
            CheckButtons(true);
            tabs.ShowDefaultCloseButton = false;
            Flush(window);
            CheckButtons(false);
            tabs.ShowDefaultCloseButton = true;
            Flush(window);
            CheckButtons(true);
            window.Width = 300;
            Flush(window);
            CheckButtons(null);
            tabs.SelectedIndex = 7;
            Flush(window);
            CheckButtons(null);
            SavePreview(tabs, $"material3-crowded-close-{withIcons}.png");
            window.Width = 200;
            Flush(window);
            CheckButtons(null);
            window.Width = 100;
            Flush(window);
            CheckButtons(false);
            window.Width = 1200;
            Flush(window);
            CheckButtons(true);

            var overrides = new TestTokenSource();
            var resources = new MaterialResource();
            resources.Children.Add(overrides);
            tabs.Resources.MergedDictionaries.Add(resources);
            // Header metrics cannot push close out even when they exhaust the entire tab.
            overrides.Set(TabsViewMaterial3Tokens.IconSize, 48d);
            overrides.Set(TabsViewMaterial3Tokens.ItemPadding, new Thickness(40, 0));
            window.Width = 760;
            Flush(window);
            CheckButtons(true);
            SavePreview(tabs, $"material3-close-priority-{withIcons}.png");
            overrides.Set(TabsViewMaterial3Tokens.CloseButtonVisibilityThreshold, 80d);
            Flush(window);
            CheckButtons(null);
            tabs.Resources.MergedDictionaries.Remove(resources);
            Flush(window);
            CheckButtons(true);

            tabs.FixedHeaderCount = 1;
            Flush(window);
            CheckButtons(true);
            window.Width = 300;
            tabs.SelectedIndex = 0;
            Flush(window);
            CheckButtons(null);
            tabs.SelectedIndex = 7;
            Flush(window);
            CheckButtons(null);
            var target = tabs.SelectedPage;
            var selectedClose = tabs.SelectedTabItem!.GetVisualDescendants().OfType<Button>()
                .Single(b => b.Name == "PART_CloseButton");
            var click = selectedClose.TranslatePoint(new Point(selectedClose.Bounds.Width / 2, selectedClose.Bounds.Height / 2), window)!.Value;
            window.MouseMove(click);
            Flush(window);
            Assert.True(selectedClose.IsPointerOver);
            window.MouseDown(click, global::Avalonia.Input.MouseButton.Left);
            window.MouseUp(click, global::Avalonia.Input.MouseButton.Left);
            Flush(window);
            Assert.Equal(9, tabs.Pages.Count());
            Assert.DoesNotContain(target, tabs.Pages);

            void CheckButtons(bool? visible)
            {
                var items = tabs.GetVisualDescendants().OfType<TabsViewItem>().ToArray();
                Assert.Equal(10, items.Length);
                foreach (var item in items)
                {
                    var close = item.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
                    var expected = (visible ?? item.IsSelected) && item.LogicalIndex >= tabs.FixedHeaderCount;
                    Assert.Equal(expected, close.IsVisible);
                    if (!expected) continue;
                    var origin = close.TranslatePoint(default, item)!.Value;
                    Assert.True(origin.X >= 0 && origin.X + close.Bounds.Width <= item.Bounds.Width,
                        "The entire close target must fit inside its tab.");
                }
            }
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void AddAndCloseUseDifferentHoverSurfacesAndRespondToPointerClicks(bool dark)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        using var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var tabs = new TabsView { InterTabController = null, RightIndent = 0 };
        var pages = (IList<Page>)tabs.Pages;
        for (var index = 0; index < 3; index++) pages.Add(new ContentPage { Header = $"Document {index}" });
        tabs.AddTabButtonClick += (_, _) => pages.Add(new ContentPage { Header = "Added by pointer" });
        var window = new Window { Width = 840, Height = 240, Content = tabs };
        AnimationScope.Initialize(window, clock);
        window.Show();
        Flush(window);
        void Frames()
        {
            for (var i = 0; i < 8; i++) { Flush(window); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); }
            Flush(window);
        }
        try
        {
            var add = tabs.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_AddItemButton");
            var addPoint = add.TranslatePoint(new Point(16, 16), window)!.Value;
            window.MouseMove(addPoint); Frames();
            var scheme = new ColorScheme { Seed = Color.Parse("#4285F4") };
            Assert.Equal(SchemeColor(scheme, MaterialTokens.Sys.Color.PrimaryContainer, dark),
                Assert.IsAssignableFrom<ISolidColorBrush>(add.Background).Color);
            SavePreview(tabs, $"material3-add-hover-{dark}.png");
            window.MouseDown(addPoint, global::Avalonia.Input.MouseButton.Left);
            window.MouseUp(addPoint, global::Avalonia.Input.MouseButton.Left); Frames();
            Assert.Equal(4, pages.Count);

            tabs.SelectedIndex = pages.Count - 1;
            Frames();
            var last = tabs.GetVisualDescendants().OfType<TabsViewItem>().OrderBy(t => t.LogicalIndex).Last();
            Assert.True(last.IsSelected);
            var close = last.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
            var closeSurface = close.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(new Size(20, 20), closeSurface.Bounds.Size);
            var sample = closeSurface.TranslatePoint(new Point(10, 3), tabs)!.Value;
            using var before = Render(tabs);
            var closePoint = close.TranslatePoint(new Point(14, 14), window)!.Value;
            window.MouseMove(closePoint); Frames();
            using var hovered = Render(tabs);
            Assert.NotEqual(Pixel(before, sample), Pixel(hovered, sample));
            SavePreview(tabs, $"material3-close-hover-{dark}.png");
            window.MouseDown(closePoint, global::Avalonia.Input.MouseButton.Left);
            window.MouseUp(closePoint, global::Avalonia.Input.MouseButton.Left); Frames();
            Assert.Equal(3, pages.Count);
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void MaterialTabsUseLiveColorTokensAndKeepIconsInsideRoundedTabs(bool dark)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var window = new Demo.MainWindow();
        window.AddFirstPage();
        window.Show();
        Flush(window);
        try
        {
            var selected = window.MyTabsView.SelectedTabItem!;
            var surface = selected.GetVisualDescendants().OfType<MaterialSurface>()
                .Single(s => s.Name == "PART_TabSurface");
            var icon = selected.GetVisualDescendants().OfType<Viewbox>().Single(v => v.Name == "PART_IconBox");
            var close = selected.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton");
            var chrome = selected.GetVisualDescendants().OfType<Decorator>().Single(c => c.Name == "PART_TabBackground");
            Assert.Equal(37, chrome.Bounds.Height);
            Assert.False(surface.IsVisible);
            Assert.Equal(new CornerRadius(12), selected.CornerRadius);
            Assert.Equal(16, icon.Bounds.Width);
            Assert.Equal(28, close.Bounds.Height);
            var defaultScheme = new ColorScheme { Seed = Color.Parse("#4285F4") };
            var contentRole = dark ? MaterialTokens.Sys.Color.SurfaceContainerHigh : MaterialTokens.Sys.Color.SurfaceContainerLowest;
            var stripRole = dark ? MaterialTokens.Sys.Color.SurfaceContainerLow : MaterialTokens.Sys.Color.SurfaceContainer;
            Assert.Equal(SchemeColor(defaultScheme, contentRole, dark),
                Assert.IsAssignableFrom<ISolidColorBrush>(window.MyTabsView.SelectedPage!.Background).Color);
            Assert.Equal(SchemeColor(defaultScheme, contentRole, dark),
                Assert.IsAssignableFrom<ISolidColorBrush>(selected.Background).Color);
            Assert.Equal(SchemeColor(defaultScheme, MaterialTokens.Sys.Color.OnSurface, dark),
                Assert.IsAssignableFrom<ISolidColorBrush>(selected.Foreground).Color);

            // The selection must follow the application's palette, including a live seed change.
            var before = Assert.IsAssignableFrom<ISolidColorBrush>(window.MyTabsView.Background).Color;
            var scheme = new ColorScheme { Seed = Colors.OrangeRed };
            var resources = new MaterialResource();
            resources.Children.Add(scheme);
            window.Resources.MergedDictionaries.Add(resources);
            Flush(window);
            Assert.NotEqual(before, Assert.IsAssignableFrom<ISolidColorBrush>(window.MyTabsView.Background).Color);
            scheme.Seed = Colors.ForestGreen;
            Flush(window);
            Assert.Equal(SchemeColor(scheme, stripRole, dark),
                Assert.IsAssignableFrom<ISolidColorBrush>(window.MyTabsView.Background).Color);

            // Both component and system key overrides must resolve in the consumer's scope.
            var overrides = new TestTokenSource();
            resources.Children.Add(overrides);
            overrides.Set(MaterialTokens.Sys.Shape.Medium, new CornerRadius(8));
            overrides.Set(TabsViewMaterial3Tokens.ContentColor, Colors.Coral);
            Flush(window);
            Assert.Equal(new CornerRadius(8), selected.CornerRadius);
            Assert.Equal(Colors.Coral, Assert.IsAssignableFrom<ISolidColorBrush>(selected.Background).Color);
            Assert.Equal(Colors.Coral, Assert.IsAssignableFrom<ISolidColorBrush>(window.MyTabsView.SelectedPage.Background).Color);
            window.Resources.MergedDictionaries.Remove(resources);
            Flush(window);

            SavePreview(window.MyTabsView, dark ? "material3-dark.png" : "material3-light.png");
            window.MyTabsView.AddItemCommand.Execute(null);
            Flush(window);
            Assert.Equal(4, window.MyTabsView.Pages.Count());
            var added = window.MyTabsView.SelectedTabItem!;
            added.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_CloseButton")
                .Command!.Execute(added);
            Flush(window);
            Assert.Equal(3, window.MyTabsView.Pages.Count());
        }
        finally
        {
            window.Close();
            app.SetTheme(Demo.DemoTheme.Fluent2);
        }
    }

    [AvaloniaTheory]
    [InlineData(4, false)]
    [InlineData(12, false)]
    [InlineData(28, false)]
    [InlineData(4, true)]
    [InlineData(12, true)]
    [InlineData(28, true)]
    public void HoverUsesTheTabOutlineAtNormalAndCrowdedWidths(int count, bool selected)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        app.RequestedThemeVariant = ThemeVariant.Light;
        using var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var tabs = new TabsView { InterTabController = null, RightIndent = 0, ShowDefaultAddButton = false };
        for (var i = 0; i < count; i++)
            ((IList<Page>)tabs.Pages).Add(new ContentPage { Header = $"Document {i}" });
        tabs.SelectedIndex = selected ? 2 : 0;
        var window = new Window { Width = 840, Height = 300, Content = tabs };
        AnimationScope.Initialize(window, clock);
        window.Show();
        Flush(window);
        try
        {
            var items = tabs.GetVisualDescendants().OfType<TabsViewItem>().OrderBy(t => t.LogicalIndex).ToArray();
            var item = items[2];
            var chrome = item.GetVisualDescendants().OfType<Decorator>().Single(c => c.Name == "PART_TabBackground");
            var surface = item.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_TabSurface");
            Assert.Same(chrome, surface.Parent);
            if (selected) Assert.False(surface.IsVisible);
            else Assert.Equal(chrome.Bounds.Size, surface.Bounds.Size);
            var leftSeparator = items[1].GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Separator");
            var rightSeparator = item.GetVisualDescendants().OfType<Border>().Single(b => b.Name == "PART_Separator");
            Assert.Equal(!selected, leftSeparator.IsVisible);
            Assert.Equal(!selected, rightSeparator.IsVisible);
            var clip = Assert.IsAssignableFrom<Geometry>(chrome.Clip);
            var foot = selected ? 12d : 0d;
            var radius = Math.Min(12, (chrome.Bounds.Width - 2 * foot) / 2);
            // Equal X and Y radii keep every top corner circular as tabs shrink.
            for (var x = 1d; x < radius; x++)
                for (var y = 1d; y < radius; y++)
                    Assert.Equal(clip.FillContains(new Point(foot + x, y)), clip.FillContains(new Point(foot + y, x)));

            var sample = chrome.TranslatePoint(new Point(chrome.Bounds.Width / 2, selected ? 34 : 3), tabs)!.Value;
            using var before = Render(tabs);
            var initial = Pixel(before, sample);
            window.MouseMove(item.TranslatePoint(new Point(item.Bounds.Width / 2, 20), window)!.Value);
            for (var frame = 0; frame < 8; frame++)
            {
                Flush(window);
                clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            }
            Flush(window);
            using var hovered = Render(tabs);
            Assert.True(item.IsPointerOver);
            Assert.False(leftSeparator.IsVisible);
            Assert.False(rightSeparator.IsVisible);
            if (selected) Assert.Equal(initial, Pixel(hovered, sample));
            else Assert.NotEqual(initial, Pixel(hovered, sample));
            // The top outside corner remains the strip color even while the layer is visible.
            var outside = chrome.TranslatePoint(new Point(foot, 0), tabs)!.Value;
            Assert.Equal(Pixel(before, outside), Pixel(hovered, outside));
            SavePreview(tabs, $"material3-hover-{count}-{selected}.png");
            window.MouseMove(new Point(420, 180));
            Flush(window);
            Assert.Equal(!selected, leftSeparator.IsVisible);
            Assert.Equal(!selected, rightSeparator.IsVisible);
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
    }

    [AvaloniaTheory]
    [InlineData(4, 1)]
    [InlineData(4, 1.25)]
    [InlineData(4, 1.5)]
    [InlineData(4, 2)]
    [InlineData(12, 1)]
    [InlineData(12, 1.25)]
    [InlineData(12, 1.5)]
    [InlineData(12, 2)]
    [InlineData(28, 1)]
    [InlineData(28, 1.25)]
    [InlineData(28, 1.5)]
    [InlineData(28, 2)]
    public void ConnectedMaterialFeetRemainCircularAndVisibleWhenTabsShrink(int count, double scale)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        app.RequestedThemeVariant = ThemeVariant.Light;
        var tabs = new TabsView { InterTabController = null, RightIndent = 0, ShowDefaultAddButton = false };
        var colors = new TestTokenSource();
        colors.Set(TabsViewMaterial3Tokens.StripColor, Colors.Black);
        colors.Set(TabsViewMaterial3Tokens.ContentColor, Colors.White);
        var resources = new MaterialResource();
        resources.Children.Add(colors);
        tabs.Resources.MergedDictionaries.Add(resources);
        for (var index = 0; index < count; index++)
            ((IList<Page>)tabs.Pages).Add(new ContentPage { Header = $"Document {index}" });
        tabs.SelectedIndex = 1;
        var window = new Window { Width = 840, Height = 100, Content = tabs };
        AnimationScope.Initialize(window);
        window.Show();
        Flush(window);
        try
        {
            var selected = tabs.SelectedTabItem!;
            var chrome = selected.GetVisualDescendants().OfType<Decorator>().Single(c => c.Name == "PART_TabBackground");
            var outline = Assert.IsAssignableFrom<Geometry>(chrome.Clip);
            var foot = 12d;
            // A quarter circle remains symmetric around its diagonal, including at crowded widths.
            for (var x = 1d; x < foot; x++)
                for (var y = 1d; y < foot; y++)
                {
                    Assert.Equal(outline.FillContains(new Point(x, chrome.Bounds.Height - foot + y)),
                        outline.FillContains(new Point(y, chrome.Bounds.Height - foot + x)));
                    Assert.Equal(outline.FillContains(new Point(chrome.Bounds.Width - x, chrome.Bounds.Height - foot + y)),
                        outline.FillContains(new Point(chrome.Bounds.Width - y, chrome.Bounds.Height - foot + x)));
                }

            var origin = selected.TranslatePoint(default, tabs)!.Value;
            var content = tabs.GetVisualDescendants().OfType<Panel>().Single(c => c.Name == "PART_ContentRoot");
            var join = content.TranslatePoint(default, tabs)!.Value;
            using var bitmap = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(840 * scale), (int)Math.Ceiling(100 * scale)),
                new Vector(96 * scale, 96 * scale));
            bitmap.Render(tabs);
            var bottom = (int)Math.Round(join.Y * scale) - 1;
            var aboveFoot = (int)Math.Floor((join.Y - foot - 1) * scale);
            foreach (var x in new[] { (int)Math.Floor(origin.X * scale) - 1,
                         (int)Math.Ceiling((origin.X + selected.Bounds.Width) * scale) })
            {
                Assert.True((Pixel(bitmap, new Point(x, bottom)) & 0xff) > 0, "The connected foot was clipped outside the tab bounds.");
                Assert.Equal(0, Pixel(bitmap, new Point(x, aboveFoot)) & 0xff);
            }
            if (scale == 1) SavePreview(tabs, $"material3-feet-{count}.png");
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
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
    public void ConnectedMaterialTabHasNoPixelSeam(double scale, bool dark)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Material3);
        app.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        var window = new Demo.MainWindow();
        window.AddFirstPage();
        window.Show();
        Flush(window);
        try
        {
            var tabs = window.MyTabsView;
            var selected = tabs.SelectedTabItem!;
            var origin = selected.TranslatePoint(default, tabs)!.Value;
            var content = tabs.GetVisualDescendants().OfType<Panel>().Single(p => p.Name == "PART_ContentRoot");
            var join = content.TranslatePoint(default, tabs)!.Value;
            var expected = Assert.IsAssignableFrom<ISolidColorBrush>(content.Background).Color;
            using var bitmap = new RenderTargetBitmap(
                new PixelSize((int)Math.Ceiling(tabs.Bounds.Width * scale), (int)Math.Ceiling(tabs.Bounds.Height * scale)),
                new Vector(96 * scale, 96 * scale));
            bitmap.Render(tabs);
            var pixel = Marshal.AllocHGlobal(4);
            try
            {
                foreach (var x in new[] { origin.X + 20, origin.X + selected.Bounds.Width / 2,
                             origin.X + selected.Bounds.Width - 20 })
                    for (var y = (int)Math.Round(join.Y * scale) - 2; y <= (int)Math.Round(join.Y * scale) + 2; y++)
                    {
                        bitmap.CopyPixels(new PixelRect((int)Math.Round(x * scale), y, 1, 1), pixel, 4, 4);
                        var bytes = new byte[4];
                        Marshal.Copy(pixel, bytes, 0, 4);
                        Assert.Equal(new[] { expected.B, expected.G, expected.R, expected.A }, bytes);
                    }
            }
            finally { Marshal.FreeHGlobal(pixel); }
        }
        finally { window.Close(); app.SetTheme(Demo.DemoTheme.Fluent2); }
    }

    [AvaloniaFact]
    public void DemoSwitchesAllThreeThemesRepeatedlyWithPageStateIntact()
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(Demo.DemoTheme.Fluent2);
        app.RequestedThemeVariant = ThemeVariant.Dark;
        var window = new Demo.MainWindow();
        window.AddFirstPage();
        var pages = (IList<Page>)window.MyTabsView.Pages;
        var page = Assert.IsType<ContentPage>(pages[1]);
        var editor = new TextBox { Text = "Material / Fluent roundtrip" };
        page.Content = editor;
        window.MyTabsView.SelectedIndex = 1;
        window.Show();
        Flush(window);
        try
        {
            foreach (var theme in new[] { 2, 0, 2, 1, 0, 1, 2, 1 })
            {
                var picker = window.GetVisualDescendants().OfType<ComboBox>().Single();
                picker.IsDropDownOpen = true;
                Flush(window);
                picker.SelectedIndex = theme;
                Flush(window);
                Assert.Equal((Demo.DemoTheme)theme, Demo.App.SelectedTheme);
                Assert.Equal(1, window.MyTabsView.SelectedIndex);
                Assert.Same(page, ((IList<Page>)window.MyTabsView.Pages)[1]);
                Assert.Same(editor, page.Content);
                Assert.Equal("Material / Fluent roundtrip", editor.Text);
                Assert.Equal(ThemeVariant.Dark, app.ActualThemeVariant);
                picker = window.GetVisualDescendants().OfType<ComboBox>().Single();
                Assert.Equal(3, picker.ItemCount);
                picker.IsDropDownOpen = true;
                Flush(window);
                Assert.All(Enumerable.Range(0, 3), index =>
                    Assert.True(Assert.IsType<ComboBoxItem>(picker.ContainerFromIndex(index)).Bounds.Height > 0));
                picker.IsDropDownOpen = false;
            }
        }
        finally
        {
            window.Close();
            app.SetTheme(Demo.DemoTheme.Fluent2);
        }
    }

    private static Color SchemeColor(ColorScheme scheme, MaterialTokenKey key, bool dark)
    {
        Assert.True(scheme.TryGetToken(key, dark ? ThemeVariant.Dark : ThemeVariant.Light, out var color));
        return Assert.IsType<Color>(color);
    }

    private static RenderTargetBitmap Render(Control control)
    {
        var bitmap = new RenderTargetBitmap(new PixelSize((int)control.Bounds.Width, (int)control.Bounds.Height));
        bitmap.Render(control);
        return bitmap;
    }

    private static int Pixel(RenderTargetBitmap bitmap, Point point)
    {
        var memory = Marshal.AllocHGlobal(4);
        try
        {
            bitmap.CopyPixels(new PixelRect((int)point.X, (int)point.Y, 1, 1), memory, 4, 4);
            return Marshal.ReadInt32(memory);
        }
        finally { Marshal.FreeHGlobal(memory); }
    }

    private sealed class TestTokenSource : ITokenSource
    {
        private readonly Dictionary<MaterialTokenKey, object> _values = [];
        public event EventHandler? Changed;
        public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => _values.TryGetValue(key, out value!);
        public void Set(MaterialTokenKey key, object value)
        {
            _values[key] = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }

    private static void SavePreview(Control control, string name)
    {
        var directory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../artifacts"));
        Directory.CreateDirectory(directory);
        using var bitmap = new RenderTargetBitmap(new PixelSize((int)control.Bounds.Width, (int)control.Bounds.Height));
        bitmap.Render(control);
        bitmap.Save(Path.Combine(directory, name));
    }
}
