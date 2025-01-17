namespace GeeksCoreLibrary.Core.Models;

public class CoreConstants
{
    public const string UrlsToSkipForMiddlewaresRegex = @"(\.jpe?g|\.gif|\.png|\.webp|\.svg|\.bmp|\.tif|\.ico|\.woff2?|\.s?css|\.js|\.[gj]cl|\.webmanifest|\.ttf)(?:\?.*)?$";

    public const string SeoTitlePropertyName = "title_seo";

    internal const string RolesDataCachingKey = "GCLRoles";
}