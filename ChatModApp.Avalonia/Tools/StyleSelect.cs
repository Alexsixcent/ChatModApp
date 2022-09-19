using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Logging;
using Avalonia.Styling;

namespace ChatModApp.Tools;

/// <summary>
/// Used to change a controls style depending on a DataContext binding value
/// </summary>
public class StyleSelect : AvaloniaObject
{
    public static readonly AttachedProperty<Styles?> StylesProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, IStyledElement, Styles?>(
            "Styles", default, false, BindingMode.OneTime);

    public static readonly AttachedProperty<object?> CurrentProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, IStyledElement, object?>(
            "Current", default, false, BindingMode.OneWay);

    public static readonly AttachedProperty<object?> KeyProperty =
        AvaloniaProperty.RegisterAttached<StyleSelect, Style, object?>(
            "Key", default, false, BindingMode.OneTime);

    private const string Name = nameof(StyleSelect);
    
    static StyleSelect()
    {
        KeyProperty.Changed.Subscribe(x => HandleKeyChanged(x.Sender, x.NewValue.GetValueOrDefault<object?>()));
        CurrentProperty.Changed.Subscribe(x => HandleCurrentChanged(x.Sender, x.NewValue.GetValueOrDefault<object?>()));
    }

    private static void HandleKeyChanged(IAvaloniaObject elem, object? value)
    {
        if (value is null) return;
        if (elem is not Style style) return; 
        
        var selClass = KeyToPseudoClass(value);
        style.Selector = style.Selector.Class(selClass);
    }

    private static void HandleCurrentChanged(IAvaloniaObject elem, object? value)
    {
        if (value is null) return;
        if (elem is not IStyledElement styledElem) return;

        var styles = styledElem.Styles;

        FindAppliedStyle(elem, styles, value, out var selected);

        if (selected is null)
        {
            Logger.TryGet(LogEventLevel.Warning, LogArea.Property)?
                .Log(elem, "Not style with corresponding key {Key} was found in {Name}.Styles",
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

    private static void FindAppliedStyle(IAvaloniaObject elem, Styles styles, object value, out Style? selected)
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
                        Logger.TryGet(LogEventLevel.Warning, LogArea.Property)?
                            .Log(elem,
                                "Style #{Index} in {Name}.Styles was skipped because it missed a {Name}.Key attached property",
                                i, Name, Name);
                        continue;
                    }

                    if (!key.Equals(value)) continue;
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