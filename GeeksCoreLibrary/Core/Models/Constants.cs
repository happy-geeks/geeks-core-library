﻿namespace GeeksCoreLibrary.Core.Models;

public class Constants
{
    // File system constants.
    public const string AppDataDirectoryName = "App_Data";
    public const string OutputCacheDirectoryName = "OutputCache";
    public const string FilesCacheDirectoryName = "FilesCache";
    public const string PublicFilesDirectoryName = "PublicFiles";

    // Keys for the JSON object that we need to read, from the "options" column in the table "wiser_entityproperty".
    public const string FieldTypeKey = "_fieldType";
    public const string SaveSeoValueKey = "_alsoSaveSeoValue";
    public const string ReadOnlyKey = "_readOnly";
    public const string SecurityMethodKey = "securityMethod";
    public const string SecurityKeyKey = "securityKey";
    public const string CultureKey = "culture";
    public const string SizeKey = "size";
    public const string SeoPropertySuffix = "_SEO";
    public const string AutoIncrementPropertySuffix = "_auto_increment";
    public const string SaveValueAsItemLinkKey = "saveValueAsItemLink";
    public const string CurrentItemIsDestinationIdKey = "currentItemIsDestinationId";
    public const string EntityTypeKey = "entityType";
    public const string LinkTypeNumberKey = "linkTypeNumber";
    public const string DefaultInputType = "text";
    public const string LinkOrderingFieldName = "__ordering";

    // Setting Constants
    public const int MinimumDefaultAwsSecretsCacheDurationInMinutes = 60;
    public const int DefaultRegexTimeoutInMilliseconds = 2000;
    public const string DefaultRegexCulture = "nl-NL";
}