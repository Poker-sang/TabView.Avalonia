using Avalonia.Controls;
using FluentIcons.Avalonia;
using FluentIcons.Common;
using System;
using System.Collections.Generic;

namespace TabView.Avalonia.Demo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // No reflection override: for AOT compilation
        MyTabsView.InterTabController = new InterTabController { InterTabClient = new AotInterTabClient() };
    }

    public void AddFirstPage()
    {
        TabsView_OnAddTabButtonClick(MyTabsView, EventArgs.Empty);
    }

    private int _newTabCount;

    private void TabsView_OnAddTabButtonClick(TabsView sender, EventArgs e)
    {
        if (sender.Pages is not IList<Page> pages)
            throw new InvalidOperationException($"{nameof(TabsView)} must use a mutable {nameof(TabsView.Pages)} collection.");

        pages.Add(new ContentPage
        {
            Header = $"New Tab {_newTabCount}",
            Icon = new SymbolIcon
            {
                IconVariant = IconVariant.Color,
                Symbol = Symbol.Home
            },
            Content = "Tab Content"
        });

        sender.SelectedIndex = pages.Count - 1;

        _newTabCount++;
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