using System;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using ImageMagick;

namespace GeeksCoreLibrary.Modules.ItemFiles.Helpers;

public static class ResizeHelpers
{
    /// <summary>
    /// Resizes an <see cref="MagickImage"/> to the specified width and height, keeping the original aspect ratio. The final result may not
    /// be in the exact dimensions.
    /// </summary>
    /// <param name="sourceBitmap">Source image as an <see cref="MagickImage"/> object.</param>
    /// <param name="width">The desired width in pixels.</param>
    /// <param name="height">The desired height in pixels.</param>
    public static void Normal(IMagickImage<byte> sourceBitmap, uint width, uint height)
    {
        var size = new MagickGeometry(width, height);
        sourceBitmap.Resize(size);
    }

    /// <summary>
    /// Resizes an <see cref="MagickImage"/> to the specified width and height, stretching the image to fill the entire image. The final result will always
    /// be in the exact dimensions of the provided width and height.
    /// </summary>
    /// <param name="sourceBitmap">Source image as an <see cref="MagickImage"/> object.</param>
    /// <param name="width">The desired width in pixels.</param>
    /// <param name="height">The desired height in pixels.</param>
    public static void Stretch(IMagickImage<byte> sourceBitmap, uint width, uint height)
    {
        var size = new MagickGeometry(width, height) {IgnoreAspectRatio = true};
        sourceBitmap.Resize(size);
    }

    /// <summary>
    /// Resizes an <see cref="MagickImage"/> to the specified width and height, and crops the image to fill the image. The final result will always be in the exact
    /// dimensions of the provided width and height.
    /// </summary>
    /// <param name="sourceBitmap">Source image as an <see cref="MagickImage"/> object.</param>
    /// <param name="width">The desired width in pixels.</param>
    /// <param name="height">The desired height in pixels.</param>
    /// <param name="anchorPosition">The position from which the source bitmap will be copied from.</param>
    public static void Crop(IMagickImage<byte> sourceBitmap, uint width, uint height, AnchorPositions anchorPosition = AnchorPositions.Center)
    {
        var biggest = Math.Max(width, height);

        // First resize the image.
        var size = new MagickGeometry(biggest, biggest) {FillArea = true};
        sourceBitmap.Resize(size);

        // Now crop the image.
        size = new MagickGeometry(width, height)
        {
            Greater = true
        };

        sourceBitmap.Crop(size, ConvertAnchorPositionToGravity(anchorPosition));
        sourceBitmap.ResetPage();
    }

    /// <summary>
    /// Resizes an <see cref="MagickImage"/> to the specified width and height, and fills the empty space with the given color, or transparent if no color is given.
    /// The final result will always be in the exact dimensions of the provided width and height.
    /// </summary>
    /// <param name="sourceBitmap">Source image as an <see cref="MagickImage"/> object.</param>
    /// <param name="width">The desired width in pixels.</param>
    /// <param name="height">The desired height in pixels.</param>
    /// <param name="anchorPosition">The position from which the source bitmap will be copied from.</param>
    /// <param name="fillColor">The color that will fill the empty space.</param>
    public static void Fill(IMagickImage<byte> sourceBitmap, uint width, uint height, AnchorPositions anchorPosition = AnchorPositions.Center, MagickColor fillColor = null)
    {
        var smallest = Math.Min(width, height);

        // First resize the image.
        Normal(sourceBitmap, smallest, smallest);

        if (fillColor == null)
        {
            // Transparent.
            fillColor = MagickColors.Transparent;
        }

        sourceBitmap.BackgroundColor = fillColor;
        sourceBitmap.Extent(width, height, ConvertAnchorPositionToGravity(anchorPosition));
    }

    private static Gravity ConvertAnchorPositionToGravity(AnchorPositions anchorPosition)
    {
        return anchorPosition switch
        {
            AnchorPositions.Center => Gravity.Center,
            AnchorPositions.Top => Gravity.North,
            AnchorPositions.Bottom => Gravity.South,
            AnchorPositions.Left => Gravity.West,
            AnchorPositions.Right => Gravity.East,
            AnchorPositions.TopLeft => Gravity.Northwest,
            AnchorPositions.TopRight => Gravity.Northeast,
            AnchorPositions.BottomRight => Gravity.Southeast,
            AnchorPositions.BottomLeft => Gravity.Southwest,
            _ => throw new ArgumentOutOfRangeException(nameof(anchorPosition), anchorPosition, null)
        };
    }
}