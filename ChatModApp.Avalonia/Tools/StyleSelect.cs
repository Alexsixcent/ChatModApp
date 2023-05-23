using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Styling;
using ChatModApp.Shared.Tools.Extensions;

namespace ChatModApp.Tools;

/// <summary>
/// Used to change a controls style depending on a DataContext binding value
/// </summary>
public class StyleSelect : AvaloniaObject
{
    public static readonly AttachedProperty<Styles?> StylesProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, StyledElement, Styles?>(
            "Styles", default, false, BindingMode.OneTime);

    public static readonly AttachedProperty<object?> CurrentProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, StyledElement, object?>(
            "Current");

    public static readonly AttachedProperty<object?> KeyProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, Style, object?>(
            "Key", default, false, BindingMode.OneTime);

    private const string Name = nameof(StyleSelect);

    private static ConditionalWeakTable<Style, Selector> OriginalStyleSelectors = new();

    static StyleSelect()
    {
        KeyProperty.Changed.Subscribe(x => HandleKeyChanged(x.Sender, x.NewValue.GetValueOrDefault<object?>()));
        CurrentProperty.Changed.Subscribe(x => HandleCurrentChanged(x.Sender, x.NewValue.GetValueOrDefault<object?>()));
    }

    private static void HandleKeyChanged(AvaloniaObject elem, object? value)
    {
        if (value is null) return;
        if (elem is not Style style) return;

        var selClass = KeyToPseudoClass(value);

        if (!style.Selector?.ToString().Contains(":style-select_") ?? false)
        {
            OriginalStyleSelectors.AddOrUpdate(style, style.Selector.Copy());
        }
        
        style.Selector = style.Selector.Class(selClass);
    }

    private static void HandleCurrentChanged(AvaloniaObject elem, object? value)
    {
        if (value is null) return;
        if (elem is not StyledElement styledElem) return;

        var curElem = styledElem;
        Style? selected;
        do
        {
            FindAppliedStyle(styledElem, curElem.Styles, value, out selected);
            if (selected is not null)
                break;

            curElem = curElem.Parent;
        } while (curElem is not null);

        if (selected is null)
        {
            Logger.TryGet(LogEventLevel.Error, LogArea.Property)?
                .Log(elem, "No style with corresponding key {Key} was found in {Name}.Styles",
                    value, Name);
            return;
        }

        var selClass = KeyToPseudoClass(value);
        var pseudo = (IPseudoClasses) styledElem.Classes;

        foreach (var @class in styledElem.Classes.Where(s => s.StartsWith(":style-select")))
        {
            pseudo.Remove(@class);
        }
        pseudo.Add(selClass);
    }

    private static void FindAppliedStyle(StyledElement elem, Styles styles, object value, out Style? selected)
    {
        selected = null;
        for (var i = 0; i < styles.Count; i++)
        {
            var style = styles[i];
            switch (style)
            {
                case Style styleElem:
                {
                    var key = styleElem.GetValue(KeyProperty);
                    if (key is null)
                    {
                        Logger.TryGet(LogEventLevel.Debug, LogArea.Property)?
                            .Log(elem,
                                "Style #{Index} in {Name}.Styles was skipped because it missed a {Name}.Key attached property",
                                i, Name, Name);
                        continue;
                    }

                    if (!key.Equals(value)) continue;

                    if (OriginalStyleSelectors.TryGetValue(styleElem, out var originSelector))
                    {
                        if (!originSelector.Match(elem, null, false).IsMatch)
                            continue;
                    }
                    else continue;

                    selected = styleElem;
                    return;
                }
                case Styles childStylesElem:
                {
                    FindAppliedStyle(elem, childStylesElem, value, out var childSel);
                    if (childSel is not null)
                    {
                        selected = childSel;
                        return;
                    }
                    continue;
                }
            }

            Logger.TryGet(LogEventLevel.Warning, LogArea.Property)?
                .Log(elem, "Style #{Index} in {Name}.Styles should be of type {StyleType}",
                    i, Name, nameof(Style));
        }
    }
    
    private static string KeyToPseudoClass(object? key) =>":style-select_" + key!.ToString()?.ToLowerInvariant();

    
    /// <summary>
    /// Accessor for Attached property <see cref="StylesProperty"/>.
    /// </summary>
    public static void SetStyles(AvaloniaObject element, Styles? stylesValue)
        => element.SetValue(StylesProperty, stylesValue);

    /// <summary>
    /// Accessor for Attached property <see cref="CurrentProperty"/>.
    /// </summary>
    public static void SetCurrent(AvaloniaObject element, object? currentValue)
        => element.SetValue(CurrentProperty, currentValue);

    /// <summary>
    /// Accessor for Attached property <see cref="KeyProperty"/>.
    /// </summary>
    public static void SetKey(AvaloniaObject element, object? keyValue)
        => element.SetValue(KeyProperty, keyValue);


    /// <summary>
    /// Accessor for Attached property <see cref="StylesProperty"/>.
    /// </summary>
    public static Styles? GetStyles(AvaloniaObject element) => element.GetValue(StylesProperty);
    
    /// <summary>
    /// Accessor for Attached property <see cref="CurrentProperty"/>.
    /// </summary>
    public static object? GetCurrent(AvaloniaObject element) => element.GetValue(CurrentProperty);
    
    /// <summary>
    /// Accessor for Attached property <see cref="KeyProperty"/>.
    /// </summary>
    public static object? GetKey(AvaloniaObject element) => element.GetValue(KeyProperty);
}