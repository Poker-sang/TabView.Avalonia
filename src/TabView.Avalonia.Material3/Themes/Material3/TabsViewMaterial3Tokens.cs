using System;
using System.Collections.Generic;
using System.Collections.Frozen;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using TabView.Avalonia.Primitives;

namespace TabView.Avalonia;

/// <summary>Component tokens for desktop document tabs. Override the key instances in a MaterialResource.</summary>
public static class TabsViewMaterial3Tokens
{
    public static readonly MaterialTokenKey<double> ItemWidth = new("tabview.material3.item.width");
    public static readonly MaterialTokenKey<double> ItemHeight = new("tabview.material3.item.height");
    public static readonly MaterialTokenKey<Thickness> ItemInset = new("tabview.material3.item.inset");
    public static readonly MaterialTokenKey<Thickness> ItemPadding = new("tabview.material3.item.padding");
    public static readonly MaterialTokenKey<CornerRadius> ItemCornerRadius = new("tabview.material3.item.corner-radius");
    public static readonly MaterialTokenKey<double> FootRadius = new("tabview.material3.item.foot-radius");
    public static readonly MaterialTokenKey<double> ContentOverlap = new("tabview.material3.item.content-overlap");
    public static readonly MaterialTokenKey<FontFamily> LabelFont = new("tabview.material3.label.font");
    public static readonly MaterialTokenKey<double> LabelSize = new("tabview.material3.label.size");
    public static readonly MaterialTokenKey<FontWeight> LabelWeight = new("tabview.material3.label.weight");
    public static readonly MaterialTokenKey<double> IconSize = new("tabview.material3.icon.size");
    public static readonly MaterialTokenKey<Thickness> IconMargin = new("tabview.material3.icon.margin");
    public static readonly MaterialTokenKey<double> CloseButtonSize = new("tabview.material3.close-button.size");
    public static readonly MaterialTokenKey<double> CloseButtonVisibilityThreshold = new("tabview.material3.close-button.visibility-threshold");
    public static readonly MaterialTokenKey<double> CloseButtonMinimumWidth = new("tabview.material3.close-button.minimum-width");
    public static readonly MaterialTokenKey<double> AddButtonSize = new("tabview.material3.add-button.size");
    public static readonly MaterialTokenKey<Thickness> CloseStateInset = new("tabview.material3.close-button.state-inset");
    public static readonly MaterialTokenKey<Color> HoverColor = new("tabview.material3.hover.color");
    public static readonly MaterialTokenKey<Color> HoverLabelColor = new("tabview.material3.hover.label.color");
    public static readonly MaterialTokenKey<Color> AddHoverColor = new("tabview.material3.add-button.hover.color");
    public static readonly MaterialTokenKey<Color> AddHoverLabelColor = new("tabview.material3.add-button.hover.label.color");
    public static readonly MaterialTokenKey<Color> StripColor = new("tabview.material3.strip.color");
    public static readonly MaterialTokenKey<Color> ContentColor = new("tabview.material3.content.color");
    public static readonly MaterialTokenKey<Color> SelectedColor = new("tabview.material3.selected.color");
    public static readonly MaterialTokenKey<Color> LabelColor = new("tabview.material3.label.color");
    public static readonly MaterialTokenKey<Color> SelectedLabelColor = new("tabview.material3.selected.label.color");
    public static readonly MaterialTokenKey<Color> SeparatorColor = new("tabview.material3.separator.color");
}

/// <summary>Default component values and references to Material3Expressive system tokens.</summary>
public sealed class TabsViewMaterial3TokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [TabsViewMaterial3Tokens.ItemWidth] = 224d,
        [TabsViewMaterial3Tokens.ItemHeight] = 40d,
        [TabsViewMaterial3Tokens.ItemInset] = new Thickness(2, 4),
        [TabsViewMaterial3Tokens.ItemPadding] = new Thickness(12, 0, 4, 0),
        [TabsViewMaterial3Tokens.ItemCornerRadius] = new TokenRef(MaterialTokens.Sys.Shape.Medium),
        [TabsViewMaterial3Tokens.FootRadius] = 12d,
        // A logical pixel overlap seals the join when the device scale is fractional.
        [TabsViewMaterial3Tokens.ContentOverlap] = 1d,
        [TabsViewMaterial3Tokens.LabelFont] = new TokenRef(MaterialTokens.Sys.TypeScale.BodyMedium.Font),
        [TabsViewMaterial3Tokens.LabelSize] = new TokenRef(MaterialTokens.Sys.TypeScale.BodyMedium.Size),
        [TabsViewMaterial3Tokens.LabelWeight] = new TokenRef(MaterialTokens.Sys.TypeScale.BodyMedium.Weight),
        [TabsViewMaterial3Tokens.IconSize] = 16d,
        [TabsViewMaterial3Tokens.IconMargin] = new Thickness(0, 0, 8, 0),
        [TabsViewMaterial3Tokens.CloseButtonSize] = 28d,
        [TabsViewMaterial3Tokens.CloseButtonVisibilityThreshold] = TabHeaderPanel.DefaultCompactWidthThreshold,
        [TabsViewMaterial3Tokens.CloseButtonMinimumWidth] = 16d,
        [TabsViewMaterial3Tokens.AddButtonSize] = 32d,
        [TabsViewMaterial3Tokens.CloseStateInset] = new Thickness(4),
        [TabsViewMaterial3Tokens.HoverColor] = new TokenRef(MaterialTokens.Sys.Color.PrimaryContainer),
        [TabsViewMaterial3Tokens.HoverLabelColor] = new TokenRef(MaterialTokens.Sys.Color.OnPrimaryContainer),
        [TabsViewMaterial3Tokens.AddHoverColor] = new TokenRef(MaterialTokens.Sys.Color.PrimaryContainer),
        [TabsViewMaterial3Tokens.AddHoverLabelColor] = new TokenRef(MaterialTokens.Sys.Color.OnPrimaryContainer),
        [TabsViewMaterial3Tokens.SelectedColor] = new TokenRef(TabsViewMaterial3Tokens.ContentColor),
        [TabsViewMaterial3Tokens.LabelColor] = new TokenRef(MaterialTokens.Sys.Color.OnSurfaceVariant),
        [TabsViewMaterial3Tokens.SelectedLabelColor] = new TokenRef(MaterialTokens.Sys.Color.OnSurface),
        [TabsViewMaterial3Tokens.SeparatorColor] = new TokenRef(MaterialTokens.Sys.Color.OutlineVariant),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value)
    {
        // Chrome's selected surface is lighter than the frame in both appearances.
        if (key == TabsViewMaterial3Tokens.StripColor)
        {
            value = new TokenRef(theme == ThemeVariant.Dark
                ? MaterialTokens.Sys.Color.SurfaceContainerLow : MaterialTokens.Sys.Color.SurfaceContainer);
            return true;
        }
        if (key == TabsViewMaterial3Tokens.ContentColor)
        {
            value = new TokenRef(theme == ThemeVariant.Dark
                ? MaterialTokens.Sys.Color.SurfaceContainerHigh : MaterialTokens.Sys.Color.SurfaceContainerLowest);
            return true;
        }
        return Values.TryGetValue(key, out value!);
    }

    public event EventHandler? Changed { add { } remove { } }
}
