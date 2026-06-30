using Avalonia.VisualTree;

namespace TabView.Avalonia;

public class DragTabControl : TabControl
{
    public static readonly DirectProperty<DragTabControl, TabsView?> OwnerProperty =
        AvaloniaProperty.RegisterDirect<DragTabControl, TabsView?>(
            nameof(Owner),
            control => control.Owner,
            (control, value) => control.Owner = value);

    public TabsView? Owner
    {
        get;
        internal set => SetAndRaise(OwnerProperty, ref field, value);
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new TabsViewItem();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) =>
        NeedsContainer<TabsViewItem>(item, out recycleKey);

    protected override void ClearContainerForItemOverride(Control element)
    {
        if (element is TabsViewItem dragTabItem)
            dragTabItem.ClearReusableContent();

        base.ClearContainerForItemOverride(element);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Owner ??= this.FindAncestorOfType<TabsView>();
        Owner?.OnTabControlTemplateApplied(this, e);
    }
}
