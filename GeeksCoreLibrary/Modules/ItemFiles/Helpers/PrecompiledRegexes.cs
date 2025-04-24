using System;
using System.Text.RegularExpressions;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.ItemFiles.Helpers;

public partial class PrecompiledRegexes
{
    [GeneratedRegex(@"^\/(?:image\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<itemId>\d+)(?:\/(?<fileType>itemlink|direct|name))?\/(?<propertyName>[^\/]+)(?:\/(?<resizeMode>normal|stretch|crop|fill)(?:-(?<anchorPosition>center|top|bottom|left|right|topleft|topright|bottomright|bottomleft))?)?(?:\/(?<preferredWidth>\d+)\/(?<preferredHeight>\d+))?(?:\/(?<fileNumber>\d+))?\/(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex ImageUrlRegex { get; }
    
    [GeneratedRegex(@"^\/(?:image\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<encryptedId>.+?)(?:\/(?<fileType>itemlink|direct|name))?\/(?<propertyName>[^\/]+)(?:\/(?<resizeMode>normal|stretch|crop|fill)(?:-(?<anchorPosition>center|top|bottom|left|right|topleft|topright|bottomright|bottomleft))?)?(?:\/(?<preferredWidth>\d+)\/(?<preferredHeight>\d+))?(?:\/(?<fileNumber>\d+))?\/(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex EncryptedImageUrlRegex { get; }
    
    [GeneratedRegex(@"^\/(?:file\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<itemId>\d+)(?:\/(?<fileType>itemlink|direct))?\/(?<propertyName>.+?)(?:\/(?<fileNumber>\d+))?(?:\/)(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex FileUrlRegex { get; }

    [GeneratedRegex(@"^\/(?:file\/wiser[0-9]?\/)(?:(?<type>[^\/]+)\/)?(?<encryptedId>.+?)(?:\/(?<fileType>itemlink|direct))?\/(?<propertyName>.+?)(?:\/(?<fileNumber>\d+))?(?:\/)(?<fileName>.+?\..+)", RegexOptions.IgnoreCase, Constants.DefaultRegexTimeoutInMilliseconds)]
    public static partial Regex EncryptedFileUrlRegex { get; }
}