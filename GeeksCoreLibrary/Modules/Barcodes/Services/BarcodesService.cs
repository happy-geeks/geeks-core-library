﻿using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Barcodes.Interfaces;
using ImageMagick;
using ImageMagick.Factories;
using ZXing;
using ZXing.Common;

namespace GeeksCoreLibrary.Modules.Barcodes.Services;

/// <inheritdoc cref="IBarcodesService"/>
public class BarcodesService : IBarcodesService, IScopedService
{
    /// <inheritdoc />
    public Task<byte[]> GenerateBarcodeAsync(string input, BarcodeFormat format, int width, int height)
    {
        var factory = new MagickImageFactory();
        var writer = new ZXing.Magick.BarcodeWriter<byte>(factory)
        {
            Format = format,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 0
            }
        };

        // Get the generated bytes.
        var fileBytes = writer.Write(input).ToByteArray();

        // Turn the bytes into a Magick image.
        using var image = new MagickImage(fileBytes);

        // Convert the Magick image to a PNG image.
        return Task.FromResult(image.ToByteArray(MagickFormat.Png));
    }
}