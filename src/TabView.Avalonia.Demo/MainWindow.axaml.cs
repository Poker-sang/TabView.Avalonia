using Avalonia;
using Avalonia.Controls;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using ComposeAnimation;
using Avalonia.Controls.Templates;
using MaterialSymbols;
using Material3Expressive.Foundation.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TabView.Avalonia.Demo;

public partial class MainWindow : Window
{
    private int _newTabCount;
    public TabsView MyTabsView { get; private set; }
    private IList<Page> PageCollection => MyTabsView.Pages as IList<Page> ??
        throw new InvalidOperationException($"{nameof(TabsView)} must use a mutable {nameof(TabsView.Pages)} collection.");

    public MainWindow()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        MyTabsView = InitialTabsView;
        Title = $"TabsView · {App.ThemeName}";
        MyTabsView.InterTabController = new InterTabController { InterTabClient = new AotInterTabClient() };
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        (Application.Current as App)?.RegisterWindow(this);
    }

    protected override void OnClosed(EventArgs e)
    {
        (Application.Current as App)?.UnregisterWindow(this);
        base.OnClosed(e);
    }

    internal TabsState DetachTabs()
    {
        var pages = PageCollection;
        var state = new TabsState(pages.ToArray(), MyTabsView.SelectedIndex,
            MyTabsView.LeftHeaderContent, MyTabsView.InterTabController);
        pages.Clear();
        MyTabsView.LeftHeaderContent = null;
        MyTabsView.RightHeaderContent = null;
        TabsHost.Children.Clear();
        return state;
    }

    internal void RestoreTabs(TabsState state)
    {
        MyTabsView = new TabsView
        {
            LeftHeaderContent = state.LeftHeader,
            RightHeaderContent = new ThemeControls(),
            LeftHeaderContentDraggable = true,
            InterTabController = state.Controller
        };
        MyTabsView.AddTabButtonClick += TabsView_OnAddTabButtonClick;
        var pages = PageCollection;
        foreach (var page in state.Pages)
        {
            pages.Add(page);
            if (page is ContentPage { Content: ShowcasePage showcase })
                showcase.RefreshThemeLabel();
        }
        MyTabsView.SelectedIndex = state.SelectedIndex;
        TabsHost.Children.Add(MyTabsView);
        Title = $"TabsView · {App.ThemeName}";
    }

    internal sealed record TabsState(Page[] Pages, int SelectedIndex, object? LeftHeader,
        InterTabController? Controller);

    public void AddFirstPage()
    {
        AddPage("Overview", Symbol.Home, new ShowcasePage());
        AddPage("Design notes", Symbol.Document, new ShowcasePage(
            "DESIGN NOTES", "Small details. Familiar rhythm.",
            "Rounded tabs, quiet dividers and subtle button states follow the WinUI 3 TabView."));
        AddPage("Release plan", Symbol.Calendar, new ShowcasePage(
            "RELEASE PLAN", "Make room for what comes next.",
            "Keep related work together. Drag a tab to reorder it, or pull it out into its own window."));
        MyTabsView.SelectedIndex = 0;
    }

    private void TabsView_OnAddTabButtonClick(TabsView sender, EventArgs e)
    {
        var number = ++_newTabCount;
        AddPage($"Untitled {number}", Symbol.Document, new ShowcasePage(
            "NEW TAB", $"A fresh start, #{number}.",
            "This page belongs to its tab. Move it between windows and pick up where you left off."));
        MyTabsView.SelectedIndex = PageCollection.Count - 1;
    }

    private void AddPage(string header, Symbol symbol, Control content)
    {
        PageCollection.Add(new ContentPage
        {
            Header = header,
            Icon = symbol,
            IconTemplate = new FuncDataTemplate<Symbol>((value, _) => App.SelectedTheme == DemoTheme.Material3
                ? new MaterialSymbol
                {
                    Symbol = value switch
                    {
                        Symbol.Home => MaterialSymbolCodepoint.Home,
                        Symbol.Calendar => MaterialSymbolCodepoint.CalendarMonth,
                        _ => MaterialSymbolCodepoint.Description
                    },
                    Width = 24,
                    Height = 24
                }
                : new SymbolIcon { Symbol = value }),
            Content = content
        });
    }

    private sealed class AotInterTabClient : IInterTabClient
    {
        public TabsHost GetNewHost(InterTabController controller, TabsHost host)
        {
            var window = new MainWindow { Owner = host.Window };
            return new TabsHost(window, window.MyTabsView);
        }
    }
}
