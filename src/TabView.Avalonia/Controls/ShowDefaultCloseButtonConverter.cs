using System;
using System.Collections.Generic;
using System.Globalization;

namespace TabView.Avalonia;

/// <summary>Combines the close button setting and fixed tab index for external tab themes.</summary>
public class ShowDefaultCloseButtonConverter : IMultiValueConverter
{
    public static readonly ShowDefaultCloseButtonConverter Instance = new();

    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // [0] is owning TabsControl ShowDefaultCloseButton value.
        // [1] is owning TabsControl FixedHeaderCount value.
        // [2] is item LogicalIndex
        if (values is not [bool showDefaultCloseButton, int fixedHeaderCount, int logicalIndex])
            return false;
        return showDefaultCloseButton && logicalIndex >= fixedHeaderCount;
    }
}
