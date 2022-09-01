using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Utilities;
using static System.Math;

namespace ChatModApp.Tools;

//From https://stackoverflow.com/a/7747002
public class AlignableWrapPanel : WrapPanel
{
    public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
        AvaloniaProperty.Register<AlignableWrapPanel, HorizontalAlignment>(
                                                                           nameof(HorizontalContentAlignment));

    public HorizontalAlignment HorizontalContentAlignment
    {
        get => GetValue(HorizontalContentAlignmentProperty);
        set => SetValue(HorizontalContentAlignmentProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var itemWidth = ItemWidth;
        var itemHeight = ItemHeight;
        var orientation = Orientation;
        var children = Children;
        var firstInLine = 0;
        var accumulatedV = 0d;
        var itemU = orientation == Orientation.Horizontal ? itemWidth : itemHeight;
        var curLineSize = new UVSize(orientation);
        var uvFinalSize = new UVSize(orientation, finalSize.Width, finalSize.Height);
        var itemWidthSet = !double.IsNaN(itemWidth);
        var itemHeightSet = !double.IsNaN(itemHeight);
        var useItemU = orientation == Orientation.Horizontal ? itemWidthSet : itemHeightSet;

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child == null) continue;

            var sz = new UVSize(orientation,
                                itemWidthSet ? itemWidth : child.DesiredSize.Width,
                                itemHeightSet ? itemHeight : child.DesiredSize.Height);

            if (MathUtilities.GreaterThan(curLineSize.U + sz.U, uvFinalSize.U)) // Need to switch to another line
            {
                ArrangeLine(accumulatedV, curLineSize, uvFinalSize, firstInLine, i, useItemU, itemU);

                accumulatedV += curLineSize.V;
                curLineSize = sz;

                if (MathUtilities
                    .GreaterThan(sz.U, uvFinalSize.U)) // The element is wider then the constraint - give it a separate line                    
                {
                    // Switch to next line which only contain one element
                    ArrangeLine(accumulatedV, sz, uvFinalSize, i, ++i, useItemU, itemU);

                    accumulatedV += sz.V;
                    curLineSize = new(orientation);
                }

                firstInLine = i;
            }
            else // Continue to accumulate a line
            {
                curLineSize.U += sz.U;
                curLineSize.V = Max(sz.V, curLineSize.V);
            }
        }

        // Arrange the last line, if any
        if (firstInLine < children.Count)
        {
            ArrangeLine(accumulatedV, curLineSize, uvFinalSize, firstInLine, children.Count, useItemU, itemU);
        }

        return finalSize;
    }

    private void ArrangeLine(double v, in UVSize lineSize, in UVSize boundsSize, int start, int end, bool useItemU,
                             double itemU)
    {
        var orientation = Orientation;
        var children = Children;
        double u = 0;
        var isHorizontal = orientation == Orientation.Horizontal;

        u = HorizontalContentAlignment switch
        {
            HorizontalAlignment.Center => (boundsSize.U - lineSize.U) / 2,
            HorizontalAlignment.Right => boundsSize.U - lineSize.U,
            _ => u
        };

        for (var i = start; i < end; i++)
        {
            var child = children[i];
            if (child is null) continue;

            var childSize = new UVSize(orientation, child.DesiredSize.Width, child.DesiredSize.Height);
            var layoutSlotU = useItemU ? itemU : childSize.U;
            child.Arrange(new(isHorizontal ? u : v,
                              isHorizontal ? v : u,
                              isHorizontal ? layoutSlotU : lineSize.U,
                              isHorizontal ? lineSize.V : layoutSlotU));
            u += layoutSlotU;
        }
    }

    private struct UVSize
    {
        internal UVSize(Orientation orientation, double width, double height)
        {
            U = V = 0d;
            _orientation = orientation;
            Width = width;
            Height = height;
        }

        internal UVSize(Orientation orientation)
        {
            U = V = 0d;
            _orientation = orientation;
        }

        internal double U;
        internal double V;
        private readonly Orientation _orientation;

        private double Width
        {
            get => _orientation == Orientation.Horizontal ? U : V;
            set
            {
                if (_orientation == Orientation.Horizontal) U = value;
                else V = value;
            }
        }

        private double Height
        {
            get => _orientation == Orientation.Horizontal ? V : U;
            set
            {
                if (_orientation == Orientation.Horizontal) V = value;
                else U = value;
            }
        }
    }
}