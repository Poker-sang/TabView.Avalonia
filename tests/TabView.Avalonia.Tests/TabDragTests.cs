using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using Xunit;

namespace TabView.Avalonia.Tests;

public class TabDragTests
{
    [AvaloniaTheory]
    [InlineData(Demo.DemoTheme.Fluent, false)]
    [InlineData(Demo.DemoTheme.Fluent, true)]
    [InlineData(Demo.DemoTheme.Fluent2, false)]
    [InlineData(Demo.DemoTheme.Fluent2, true)]
    [InlineData(Demo.DemoTheme.Material3, false)]
    [InlineData(Demo.DemoTheme.Material3, true)]
    public void HeaderDragKeepsCaptureOutsideTheTabAndCanTearOffAndMerge(Demo.DemoTheme theme, bool selected)
    {
        var app = Assert.IsType<Demo.App>(Application.Current);
        app.SetTheme(theme);
        var client = new RecordingInterTabClient();
        var tabs = CreateTabs();
        tabs.InterTabController = new InterTabController
        {
            InterTabClient = client,
            Partition = Guid.NewGuid().ToString()
        };
        var editor = new TextBox { Text = "Keep this page through tear-off and merge" };
        var page = new ContentPage
        {
            Header = "Drag this document",
            Icon = new Border { Width = 16, Height = 16, Background = Brushes.Red },
            Content = editor
        };
        var pages = Assert.IsAssignableFrom<IList<Page>>(tabs.Pages);
        pages.Add(new ContentPage { Header = "First" });
        pages.Add(page);
        pages.Add(new ContentPage { Header = "Last" });
        tabs.SelectedIndex = selected ? 1 : 0;
        var window = CreateWindow(tabs);
        window.Show();
        Flush(window);
        try
        {
            var item = tabs.GetVisualDescendants().OfType<TabsViewItem>().Single(i => i.LogicalIndex == 1);
            var grab = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
            window.MouseMove(grab);
            window.MouseDown(grab, MouseButton.Left);
            Assert.Same(page, tabs.SelectedPage);

            // Leave the tab before crossing the strip's tear-off grace distance.
            // A clickable feedback layer must not cancel the thumb's capture here.
            window.MouseMove(grab + new Vector(10, 30), RawInputModifiers.LeftMouseButton);
            Flush(window);
            Assert.True(item.IsDragging, "The header must retain its drag gesture outside the tab bounds.");
            Assert.Empty(client.Hosts);

            window.MouseMove(grab + new Vector(10, 140), RawInputModifiers.LeftMouseButton);
            Flush(window);
            var floating = Assert.Single(client.Hosts);
            Flush(floating.Window);
            Assert.Equal(2, pages.Count);
            Assert.Same(page, Assert.Single(floating.TabsControl.Pages!));
            Assert.Same(editor, page.Content);

            // The source retains capture while the floating window follows the pointer.
            var reentry = new Point(100, grab.Y);
            window.MouseMove(reentry, RawInputModifiers.LeftMouseButton);
            Flush(window);
            Assert.Equal(3, pages.Count);
            Assert.Same(page, tabs.SelectedPage);
            Assert.False(floating.Window.IsVisible);
            window.MouseUp(reentry, MouseButton.Left);
            Flush(window);
            Assert.All(tabs.GetVisualDescendants().OfType<TabsViewItem>(), i => Assert.False(i.IsDragging));
            Assert.Same(editor, page.Content);
            Assert.Equal("Keep this page through tear-off and merge", editor.Text);

            // After merging, start another drag over the icon and reorder the same page.
            var merged = tabs.SelectedTabItem!;
            var iconGrab = merged.TranslatePoint(new Point(24, merged.Bounds.Height / 2), window)!.Value;
            var lastTab = new Point(merged.Bounds.Width * 2.5, iconGrab.Y);
            window.MouseMove(iconGrab);
            window.MouseDown(iconGrab, MouseButton.Left);
            window.MouseMove(lastTab, RawInputModifiers.LeftMouseButton);
            Flush(window);
            window.MouseUp(lastTab, MouseButton.Left);
            Flush(window);
            Assert.Same(page, pages[^1]);
            Assert.Same(editor, page.Content);
        }
        finally
        {
            foreach (var host in client.Hosts) host.Window.Close();
            window.Close();
            app.SetTheme(Demo.DemoTheme.Fluent2);
        }
    }

    private static TabsView CreateTabs() => new() { RightIndent = 0, ShowDefaultAddButton = false };

    private static Window CreateWindow(TabsView tabs)
    {
        var window = new Window { Width = 900, Height = 300 };
        AnimationScope.Initialize(window);
        window.Content = tabs;
        return window;
    }

    private sealed class RecordingInterTabClient : IInterTabClient
    {
        public List<TabsHost> Hosts { get; } = [];

        public TabsHost GetNewHost(InterTabController controller, TabsHost host)
        {
            var tabs = CreateTabs();
            var result = new TabsHost(CreateWindow(tabs), tabs);
            Hosts.Add(result);
            return result;
        }
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}
