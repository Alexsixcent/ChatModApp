// Copyright (c) Six Labors.
// Licensed under the Six Labors Split License.

using System;
using System.Runtime.CompilerServices;
using Avalonia;
using SixLabors.ImageSharp.Metadata;

namespace AvaloniaGif;

/// <summary>
/// Contains methods for converting values between unit scales.
/// </summary>
internal static class UnitConverter
{
    /// <summary>
    /// The number of centimeters in a meter.
    /// 1 cm is equal to exactly 0.01 meters.
    /// </summary>
    private const double CmsInMeter = 1 / 0.01D;

    /// <summary>
    /// The number of centimeters in an inch.
    /// 1 inch is equal to exactly 2.54 centimeters.
    /// </summary>
    private const double CmsInInch = 2.54D;

    /// <summary>
    /// The number of inches in a meter.
    /// 1 inch is equal to exactly 0.0254 meters.
    /// </summary>
    private const double InchesInMeter = 1 / 0.0254D;

    /// <summary>
    /// The default resolution unit value.
    /// </summary>
    private const PixelResolutionUnit DefaultResolutionUnit = PixelResolutionUnit.PixelsPerInch;

    /// <summary>
    /// Scales the value from centimeters to meters.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CmToMeter(double x) => x * CmsInMeter;

    /// <summary>
    /// Scales the value from meters to centimeters.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MeterToCm(double x) => x / CmsInMeter;

    /// <summary>
    /// Scales the value from meters to inches.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double MeterToInch(double x) => x / InchesInMeter;

    /// <summary>
    /// Scales the value from inches to meters.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InchToMeter(double x) => x * InchesInMeter;

    /// <summary>
    /// Scales the value from centimeters to inches.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CmToInch(double x) => x / CmsInInch;

    /// <summary>
    /// Scales the value from inches to centimeters.
    /// </summary>
    /// <param name="x">The value to scale.</param>
    /// <returns>The <see cref="double"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double InchToCm(double x) => x * CmsInInch;

    public static Vector GetImageDpi(this ImageMetadata metadata)
    {
        return metadata.ResolutionUnits switch
        {
            PixelResolutionUnit.AspectRatio => new(96 * metadata.HorizontalResolution, 96 * metadata.VerticalResolution),
            PixelResolutionUnit.PixelsPerInch => new(metadata.HorizontalResolution, metadata.VerticalResolution),
            PixelResolutionUnit.PixelsPerCentimeter => new(CmToInch(metadata.HorizontalResolution),
                                                           CmToInch(metadata.VerticalResolution)),
            PixelResolutionUnit.PixelsPerMeter => new(MeterToInch(metadata.HorizontalResolution),
                                                      MeterToInch(metadata.VerticalResolution)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}
