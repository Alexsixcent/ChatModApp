using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Utilities;

namespace ChatModApp.Controls;

public class OptimizedArc : Control
{
    /// <summary>
    /// Defines the <see cref="Stretch"/> property.
    /// </summary>
    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<Shape, Stretch>(nameof(Stretch));

    /// <summary>
    /// Defines the <see cref="Stroke"/> property.
    /// </summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<Shape, IBrush?>(nameof(Stroke));


    /// <summary>
    /// Defines the <see cref="StrokeThickness"/> property.
    /// </summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<Shape, double>(nameof(StrokeThickness));

    /// <summary>
    /// Defines the <see cref="StrokeLineCap"/> property.
    /// </summary>
    public static readonly StyledProperty<PenLineCap> StrokeLineCapProperty =
        AvaloniaProperty.Register<Shape, PenLineCap>(nameof(StrokeLineCap), PenLineCap.Flat);


    /// <summary>
    /// Defines the <see cref="StartAngle"/> property.
    /// </summary>
    public static readonly StyledProperty<double> StartAngleProperty =
        AvaloniaProperty.Register<Arc, double>(nameof(StartAngle), 0.0);

    /// <summary>
    /// Defines the <see cref="SweepAngle"/> property.
    /// </summary>
    public static readonly StyledProperty<double> SweepAngleProperty =
        AvaloniaProperty.Register<Arc, double>(nameof(SweepAngle), 0.0);

    public static readonly StyledProperty<double> RotationAngleProperty =
        AvaloniaProperty.Register<OptimizedArc, double>(
                                                        nameof(RotationAngle));


    /// <summary>
    /// Gets or sets a <see cref="Stretch"/> enumeration value that describes how the shape fills its allocated space.
    /// </summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>
    /// Gets or sets the <see cref="IBrush"/> that specifies how the shape's outline is painted.
    /// </summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>
    /// Gets or sets the width of the shape outline.
    /// </summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>
    /// Gets or sets a <see cref="PenLineCap"/> enumeration value that describes the shape at the ends of a line.
    /// </summary>
    public PenLineCap StrokeLineCap
    {
        get => GetValue(StrokeLineCapProperty);
        set => SetValue(StrokeLineCapProperty, value);
    }

    /// <summary>
    /// Gets or sets the angle at which the arc starts, in degrees.
    /// </summary>
    public double StartAngle
    {
        get => GetValue(StartAngleProperty);
        set => SetValue(StartAngleProperty, value);
    }

    /// <summary>
    /// Gets or sets the angle, in degrees, added to the <see cref="StartAngle"/> defining where the arc ends.
    /// A positive value is clockwise, negative is counter-clockwise.
    /// </summary>
    public double SweepAngle
    {
        get => GetValue(SweepAngleProperty);
        set => SetValue(SweepAngleProperty, value);
    }

    public double RotationAngle
    {
        get => GetValue(RotationAngleProperty);
        set => SetValue(RotationAngleProperty, value);
    }
    
    /// <summary>
    /// Gets a value that represents the <see cref="Geometry"/> of the shape.
    /// </summary>
    public Geometry DefiningGeometry => _definingGeometry ??= CreateDefiningGeometry();

    /// <summary>
    /// Gets a value that represents the final rendered <see cref="Geometry"/> of the shape.
    /// </summary>
    public Geometry? RenderedGeometry
    {
        get
        {
            if (_renderedGeometry is not null) return _renderedGeometry;
            if (_transform == Matrix.Identity)
            {
                _renderedGeometry = DefiningGeometry;
            }
            else
            {
                _renderedGeometry = DefiningGeometry.Clone();

                if (_renderedGeometry.Transform == null ||
                    _renderedGeometry.Transform.Value == Matrix.Identity)
                {
                    _renderedGeometry.Transform = new MatrixTransform(_transform);
                }
                else
                {
                    _renderedGeometry.Transform = new MatrixTransform(_renderedGeometry.Transform.Value * _transform);
                }
            }

            return _renderedGeometry;
        }
    }

    private Geometry? _definingGeometry;
    private Geometry? _renderedGeometry;
    private Matrix _transform = Matrix.Identity;

    static OptimizedArc()
    {
        AffectsMeasure<OptimizedArc>(StretchProperty);
        AffectsArrange<OptimizedArc>(RotationAngleProperty);
        AffectsRender<OptimizedArc>(StartAngleProperty, 
                                    SweepAngleProperty, 
                                    StrokeProperty, 
                                    StrokeThicknessProperty, 
                                    StrokeLineCapProperty,
                                    RotationAngleProperty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == StartAngleProperty
            || change.Property == SweepAngleProperty
            || change.Property == StrokeProperty
            || change.Property == StrokeThicknessProperty
            || change.Property == StrokeLineCapProperty)
        {
            _definingGeometry = CreateDefiningGeometry();
            _renderedGeometry = null;
        }

        if (change.Property == RotationAngleProperty)
        {
            _renderedGeometry = null;
        }
        
        base.OnPropertyChanged(change);
    }

    public sealed override void Render(DrawingContext context)
    {
        var geometry = RenderedGeometry;

        if (geometry is null)
            return;

        var stroke = Stroke;

        ImmutablePen? pen = null;

        if (stroke is not null)
        {
            ImmutableDashStyle? dashStyle = null;

            pen = new(stroke.ToImmutable(),
                      StrokeThickness,
                      dashStyle,
                      StrokeLineCap);
        }

        context.DrawGeometry(null, pen, geometry);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        return CalculateSizeAndTransform(availableSize, DefiningGeometry.Bounds, Stretch).size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var (_, transform) = CalculateSizeAndTransform(finalSize, DefiningGeometry.Bounds, Stretch);

        if (_transform == transform) return finalSize;

        _transform = transform;
        _renderedGeometry = null;

        return finalSize;
    }

    private Geometry CreateDefiningGeometry()
    {
        var angle1 = MathUtilities.Deg2Rad(StartAngle);
        var angle2 = angle1 + MathUtilities.Deg2Rad(SweepAngle);

        var startAngle = Math.Min(angle1, angle2);
        var sweepAngle = Math.Max(angle1, angle2);

        var normStart = RadToNormRad(startAngle);
        var normEnd = RadToNormRad(sweepAngle);

        var rect = new Rect(Bounds.Size);

        if ((normStart == normEnd) && (startAngle != sweepAngle)) // Complete ring.
        {
            return new EllipseGeometry(rect.Deflate(StrokeThickness / 2));
        }

        if (SweepAngle == 0)
        {
            return new StreamGeometry();
        }

        // Partial arc.
        var deflatedRect = rect.Deflate(StrokeThickness / 2);

        var centerX = rect.Center.X;
        var centerY = rect.Center.Y;

        var radiusX = deflatedRect.Width / 2;
        var radiusY = deflatedRect.Height / 2;

        var angleGap = RadToNormRad(sweepAngle - startAngle);

        var startPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, startAngle);
        var endPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, sweepAngle);

        var arcGeometry = new StreamGeometry();

        using var context = arcGeometry.Open();

        context.BeginFigure(startPoint, false);
        context.ArcTo(
                      endPoint,
                      new(radiusX, radiusY),
                      rotationAngle: angleGap,
                      isLargeArc: angleGap >= Math.PI,
                      SweepDirection.Clockwise);
        context.EndFigure(false);

        return arcGeometry;
    }

    private (Size size, Matrix transform) CalculateSizeAndTransform(
        Size availableSize, Rect shapeBounds, Stretch stretch)
    {
        var shapeSize = new Size(shapeBounds.Right, shapeBounds.Bottom);
        var translate = Matrix.Identity;
        var rotation = Matrix.Identity;
        var desiredX = availableSize.Width;
        var desiredY = availableSize.Height;
        var sx = 0.0;
        var sy = 0.0;

        if (stretch != Stretch.None)
        {
            shapeSize = shapeBounds.Size;
            translate = Matrix.CreateTranslation(-(Vector)shapeBounds.Position);
        }

        if (RotationAngle != 0)
        {
             rotation = Matrix.CreateRotation(MathUtilities.Deg2Rad(RotationAngle));
        }

        if (double.IsInfinity(availableSize.Width))
        {
            desiredX = shapeSize.Width;
        }

        if (double.IsInfinity(availableSize.Height))
        {
            desiredY = shapeSize.Height;
        }

        if (shapeBounds.Width > 0)
        {
            sx = desiredX / shapeSize.Width;
        }

        if (shapeBounds.Height > 0)
        {
            sy = desiredY / shapeSize.Height;
        }

        if (double.IsInfinity(availableSize.Width))
        {
            sx = sy;
        }

        if (double.IsInfinity(availableSize.Height))
        {
            sy = sx;
        }

        switch (stretch)
        {
            case Stretch.Uniform:
                sx = sy = Math.Min(sx, sy);
                break;
            case Stretch.UniformToFill:
                sx = sy = Math.Max(sx, sy);
                break;
            case Stretch.Fill:
                if (double.IsInfinity(availableSize.Width))
                {
                    sx = 1.0;
                }

                if (double.IsInfinity(availableSize.Height))
                {
                    sy = 1.0;
                }

                break;
            default:
                sx = sy = 1;
                break;
        }

        var transform = translate * rotation * Matrix.CreateScale(sx, sy);
        var size = new Size(shapeSize.Width * sx, shapeSize.Height * sy);
        return (size, transform);
    }

    private static double RadToNormRad(double inAngle) => ((inAngle % (Math.PI * 2)) + (Math.PI * 2)) % (Math.PI * 2);

    private static Point GetRingPoint(double radiusX, double radiusY, double centerX, double centerY, double angle) =>
        new((radiusX * Math.Cos(angle)) + centerX, (radiusY * Math.Sin(angle)) + centerY);
}