using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GeeksCoreLibrary.Modules.ItemFiles.Enums;

[JsonConverter(typeof(StringEnumConverter))]
public enum ResizeModes
{
    /// <summary>
    /// Resize the image, keeping aspect ratio intact.
    /// </summary>
    Normal,
    /// <summary>
    /// Resize the image, stretching the width or height to fill the entire image.
    /// </summary>
    Stretch,
    /// <summary>
    /// Resize the image, cropping the parts that don't fit the bounds.
    /// </summary>
    Crop,
    /// <summary>
    /// Resize the image, keeping aspect ratio intact. Empty parts will be filled with transparent pixels, or white pixels if the image format doesn't support transparency.
    /// </summary>
    Fill
}