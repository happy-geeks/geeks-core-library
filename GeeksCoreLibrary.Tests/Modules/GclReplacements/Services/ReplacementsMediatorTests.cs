using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Services;
using Moq;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Modules.GclReplacements.Services;

public class ReplacementsMediatorTests
{
    #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReplacementsMediator replacementsMediator;
    private Mock<IDatabaseConnection> databaseConnectionMock;
    #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [SetUp]
    public void Setup()
    {
        databaseConnectionMock = new Mock<IDatabaseConnection>();
        replacementsMediator = new ReplacementsMediator(databaseConnectionMock.Object);
    }

    [Test]
    [TestCase("")]
    [TestCase("NotExistingMethod")]
    public void GetFormatterMethod_InvalidFormatters_ReturnsNull(string replacementString)
    {
        // Act
        var actual = replacementsMediator.GetFormatterMethod(replacementString);

        // Assert
        actual.Should().BeNull("because we're testing strings that don't contain any formatters");
    }

    #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
    [Test]
    [TestCase("Raw", "Raw", null)]
    [TestCase("Seo", "Seo", null)]
    [TestCase("HtmlEncode", "HtmlEncode", null)]
    [TestCase("UrlEncode", "UrlEncode", null)]
    [TestCase("UrlDecode", "UrlDecode", null)]
    [TestCase("CutString(5)", "CutString", new object[] { 5, "" })]
    [TestCase("CutString(5,test)", "CutString", new object[] { 5, "test" })]
    [TestCase("FormatNumber", "FormatNumber", new object[] { "N2", "nl-NL" })]
    [TestCase("Currency", "Currency", new object[] { true, null })]
    [TestCase("Currency(false)", "Currency", new object[] { false, null })]
    [TestCase("Currency(false, en-US)", "Currency", new object[] { false, "en-US" })]
    [TestCase("CurrencySup", "CurrencySup", new object[] { true, null })]
    [TestCase("CurrencySup(false)", "CurrencySup", new object[] { false, null })]
    [TestCase("CurrencySup(false, en-US)", "CurrencySup", new object[] { false, "en-US" })]
    [TestCase("UppercaseFirst", "UppercaseFirst", null)]
    [TestCase("LowercaseFirst", "LowercaseFirst", null)]
    [TestCase("StripHtml", "StripHtml", null)]
    [TestCase("JsonSafe", "JsonSafe", null)]
    [TestCase("StripInlineStyle", "StripInlineStyle", null)]
    [TestCase("Encrypt", "Encrypt", new object[] { false })]
    [TestCase("Encrypt(true)", "Encrypt", new object[] { true })]
    [TestCase("EncryptNormal", "EncryptNormal", new object[] { false })]
    [TestCase("EncryptNormal(true)", "EncryptNormal", new object[] { true })]
    [TestCase("Decrypt", "Decrypt", new object[] { false, 0 })]
    [TestCase("Decrypt(true)", "Decrypt", new object[] { true, 0 })]
    [TestCase("Decrypt(true,5)", "Decrypt", new object[] { true, 5 })]
    [TestCase("DecryptNormal", "DecryptNormal", new object[] { false, 0 })]
    [TestCase("DecryptNormal(true)", "DecryptNormal", new object[] { true, 0 })]
    [TestCase("DecryptNormal(true,5)", "DecryptNormal", new object[] { true, 5 })]
    [TestCase("Base64", "Base64", null)]
    [TestCase("DateTime(yyyy-MM-dd)", "DateTime", new object[] { "yyyy-MM-dd", null })]
    [TestCase("DateTime(yyyy-MM-dd, nl-NL)", "DateTime", new object[] { "yyyy-MM-dd", "nl-NL" })]
    [TestCase("Sha512", "Sha512", null)]
    [TestCase("Hash(SHA512, Base64)", "Hash", new object[] { "SHA512", "Base64" })]
    [TestCase("Uppercase", "Uppercase", new object[] { false })]
    [TestCase("Uppercase(true)", "Uppercase", new object[] { true })]
    [TestCase("Lowercase", "Lowercase", new object[] { false })]
    [TestCase("Lowercase(true)", "Lowercase", new object[] { true })]
    [TestCase("QrCode(1,1)", "QrCode", new object[] { 1, 1 })]
    public void GetFormatterMethod_DifferentStrings_ReturnsCorrectFormatter(string formatterString, string expectedFormatterName, object[] expectedParameters)
    {
        // Act
        var actual = replacementsMediator.GetFormatterMethod(formatterString);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a formatter method");
        actual.Method.Name.Should().Be(expectedFormatterName, $"because we expected the name to be {expectedFormatterName}");
        actual.Parameters.Should().BeEquivalentTo(expectedParameters);
    }

    [Test]
    [TestCase("")]
    [TestCase("String without variables")]
    [TestCase("String with {invalid variable")]
    [TestCase("String with invalid} variable")]
    [TestCase("String with invalid {} variable")]
    public void GetReplacementVariables_StringsWithoutVariables_ReturnsEmptyList(string input)
    {
        // Act
        var actual = replacementsMediator.GetReplacementVariables(input);

        // Assert
        actual.Should().BeEmpty("because we're testing strings that don't contain any replacement variables");
    }

    [Test]
    [TestCase("String with {valid} variable", "{valid}", "valid", "valid", new[] { "HtmlEncode" }, "")]
    [TestCase("String with {Valid:Hash(SHA512, Base64)} variable", "{Valid:Hash(SHA512, Base64)}", "Valid", "Valid:Hash(SHA512, Base64)", new[] { "HtmlEncode", "Hash(SHA512, Base64)" }, "")]
    [TestCase("String with {valid:Raw} variable", "{valid:Raw}", "valid", "valid:Raw", new[] { "Raw" }, "")]
    [TestCase("String with {valid~DefaultValue} variable", "{valid~DefaultValue}", "valid", "valid~DefaultValue", new[] { "HtmlEncode" }, "DefaultValue")]
    [TestCase("String with {valid~DefaultValue:Raw} variable", "{valid~DefaultValue:Raw}", "valid", "valid~DefaultValue:Raw", new[] { "Raw" }, "DefaultValue")]
    [TestCase("String with {valid:Raw~DefaultValue} variable", "{valid:Raw~DefaultValue}", "valid", "valid:Raw~DefaultValue", new[] { "Raw" }, "DefaultValue")]
    [TestCase("String with {Valid~DefaultValue:Hash(SHA512, Base64)} variable", "{Valid~DefaultValue:Hash(SHA512, Base64)}", "Valid", "Valid~DefaultValue:Hash(SHA512, Base64)", new[] { "HtmlEncode", "Hash(SHA512, Base64)" }, "DefaultValue")]
    [TestCase("String with {Valid:Hash(SHA512, Base64)~DefaultValue} variable", "{Valid:Hash(SHA512, Base64)~DefaultValue}", "Valid", "Valid:Hash(SHA512, Base64)~DefaultValue", new[] { "HtmlEncode", "Hash(SHA512, Base64)" }, "DefaultValue")]
    [TestCase("String with {valid:Seo} variable", "{valid:Seo}", "valid", "valid:Seo", new[] { "HtmlEncode", "Seo" }, "")]
    [TestCase("String with {valid:HtmlEncode} variable", "{valid:HtmlEncode}", "valid", "valid:HtmlEncode", new[] { "HtmlEncode" }, "")]
    [TestCase("String with {valid:UrlEncode} variable", "{valid:UrlEncode}", "valid", "valid:UrlEncode", new[] { "UrlEncode" }, "")]
    [TestCase("String with {valid:UrlDecode} variable", "{valid:UrlDecode}", "valid", "valid:UrlDecode", new[] { "HtmlEncode", "UrlDecode" }, "")]
    [TestCase("String with {valid:CutString(10, test)} variable", "{valid:CutString(10, test)}", "valid", "valid:CutString(10, test)", new[] { "HtmlEncode", "CutString(10, test)" }, "")]
    [TestCase("String with {valid:FormatNumber(C, en-UK)} variable", "{valid:FormatNumber(C, en-UK)}", "valid", "valid:FormatNumber(C, en-UK)", new[] { "HtmlEncode", "FormatNumber(C, en-UK)" }, "")]
    [TestCase("String with {valid:Currency(false, en-UK)} variable", "{valid:Currency(false, en-UK)}", "valid", "valid:Currency(false, en-UK)", new[] { "HtmlEncode", "Currency(false, en-UK)" }, "")]
    [TestCase("String with {valid:CurrencySup(false, en-UK)} variable", "{valid:CurrencySup(false, en-UK)}", "valid", "valid:CurrencySup(false, en-UK)", new[] { "CurrencySup(false, en-UK)" }, "")]
    [TestCase("String with {valid:UppercaseFirst} variable", "{valid:UppercaseFirst}", "valid", "valid:UppercaseFirst", new[] { "HtmlEncode", "UppercaseFirst" }, "")]
    [TestCase("String with {valid:LowercaseFirst} variable", "{valid:LowercaseFirst}", "valid", "valid:LowercaseFirst", new[] { "HtmlEncode", "LowercaseFirst" }, "")]
    [TestCase("String with {valid:StripHtml} variable", "{valid:StripHtml}", "valid", "valid:StripHtml", new[] { "HtmlEncode", "StripHtml" }, "")]
    [TestCase("String with {valid:JsonSafe} variable", "{valid:JsonSafe}", "valid", "valid:JsonSafe", new[] { "HtmlEncode", "JsonSafe" }, "")]
    [TestCase("String with {valid:StripInlineStyle} variable", "{valid:StripInlineStyle}", "valid", "valid:StripInlineStyle", new[] { "HtmlEncode", "StripInlineStyle" }, "")]
    [TestCase("String with {valid:Encrypt(true)} variable", "{valid:Encrypt(true)}", "valid", "valid:Encrypt(true)", new[] { "HtmlEncode", "Encrypt(true)" }, "")]
    [TestCase("String with {valid:EncryptNormal(true)} variable", "{valid:EncryptNormal(true)}", "valid", "valid:EncryptNormal(true)", new[] { "HtmlEncode", "EncryptNormal(true)" }, "")]
    [TestCase("String with {valid:Decrypt(true)} variable", "{valid:Decrypt(true)}", "valid", "valid:Decrypt(true)", new[] { "HtmlEncode", "Decrypt(true)" }, "")]
    [TestCase("String with {valid:DecryptNormal(true)} variable", "{valid:DecryptNormal(true)}", "valid", "valid:DecryptNormal(true)", new[] { "HtmlEncode", "DecryptNormal(true)" }, "")]
    [TestCase("String with {valid:Base64} variable", "{valid:Base64}", "valid", "valid:Base64", new[] { "HtmlEncode", "Base64" }, "")]
    [TestCase("String with {valid:DateTime(yyyy-MM-dd, nl-NL)} variable", "{valid:DateTime(yyyy-MM-dd, nl-NL)}", "valid", "valid:DateTime(yyyy-MM-dd, nl-NL)", new[] { "HtmlEncode", "DateTime(yyyy-MM-dd, nl-NL)" }, "")]
    [TestCase("String with {valid:Sha512} variable", "{valid:Sha512}", "valid", "valid:Sha512", new[] { "HtmlEncode", "Sha512" }, "")]
    [TestCase("String with {valid:Hash(SHA512, Base64)} variable", "{valid:Hash(SHA512, Base64)}", "valid", "valid:Hash(SHA512, Base64)", new[] { "HtmlEncode", "Hash(SHA512, Base64)" }, "")]
    [TestCase("String with {valid:Uppercase(true)} variable", "{valid:Uppercase(true)}", "valid", "valid:Uppercase(true)", new[] { "HtmlEncode", "Uppercase(true)" }, "")]
    [TestCase("String with {valid:Lowercase(true)} variable", "{valid:Lowercase(true)}", "valid", "valid:Lowercase(true)", new[] { "HtmlEncode", "Lowercase(true)" }, "")]
    [TestCase("String with {valid:QrCode(100, 100)} variable", "{valid:QrCode(100, 100)}", "valid", "valid:QrCode(100, 100)", new[] { "HtmlEncode", "QrCode(100, 100)" }, "")]
    public void GetReplacementVariables_StringsWithSingleVariable_ReturnsReplacementVariablesArrayWithAtLeastOneItem(string input, string matchString, string variableName, string originalVariableName, ICollection<string> formatters, string defaultValue)
    {
        // Act
        var actual = replacementsMediator.GetReplacementVariables(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a replacement variable");
        actual.Should().NotBeEmpty("because we're testing specific strings that should always return a replacement variable");
        actual.Length.Should().Be(1, "because we're testing specific strings that should always return a single replacement variable");
        actual[0].MatchString.Should().Be(matchString, $"because we expected the match string to be {matchString}");
        actual[0].VariableName.Should().Be(variableName, $"because we expected the variable name to be {variableName}");
        actual[0].OriginalVariableName.Should().Be(originalVariableName, $"because we expected the original variable name to be {originalVariableName}");
        actual[0].Formatters.Should().BeEquivalentTo(formatters);
        actual[0].DefaultValue.Should().Be(defaultValue, $"because we expected the default value to be {defaultValue}");
    }

    [Test]
    [TestCase("This is a {test} string with {multiple} variables.", new[] { "{test}", "{multiple}" }, new[] { "test", "multiple" }, new[] { "test", "multiple" }, new[] { "", "" }, new[] { "HtmlEncode" }, new[] { "HtmlEncode" })]
    [TestCase("This is a {test:Raw} string with {multiple:Uppercase} variables.", new[] { "{test:Raw}", "{multiple:Uppercase}" }, new[] { "test", "multiple" }, new[] { "test:Raw", "multiple:Uppercase" }, new[] { "", "" }, new [] { "Raw" }, new[] { "HtmlEncode", "Uppercase" })]
    // TODO: This test doesn't work yet, finish/fix it.
    [TestCase("This is a {test~DefaultValue1} string with {multiple:Uppercase~DefaultValue2} {variables~~DefaultValue3:Lowercase}.", new[] { "{test~DefaultValue1}", "{multiple:Uppercase~DefaultValue2}", "{variables~~DefaultValue3:Lowercase}" }, new[] { "test", "multiple", "variables" }, new[] { "test", "multiple:Uppercase", "variables:Lowercase" }, new[] { "DefaultValue1", "DefaultValue2", "DefaultValue3" }, new [] { "HtmlEncode" }, new[] { "HtmlEncode", "Uppercase" }, new[] { "HtmlEncode", "Lowercase" })]
    public void GetReplacementVariables_StringsWithMultipleVariables_ReturnsReplacementVariablesArrayWithMultipleItems(string input, ICollection<string> matchStrings, ICollection<string> variableNames, ICollection<string> originalVariableNames, ICollection<string> defaultValues, params string[][] formatters)
    {
        // Act
        var actual = replacementsMediator.GetReplacementVariables(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a replacement variable");
        actual.Should().NotBeEmpty("because we're testing specific strings that should always return a replacement variable");
        actual.Length.Should().Be(matchStrings.Count, "because we're testing specific strings that should always return a specific amount of replacement variables");

        actual.Select(x => x.MatchString).ToArray().Should().BeEquivalentTo(matchStrings, $"because we expected the match string to be {String.Join(", ", matchStrings)}");
        actual.Select(x => x.VariableName).ToArray().Should().BeEquivalentTo(variableNames, $"because we expected the variable name to be {String.Join(", ", variableNames)}");
        actual.Select(x => x.OriginalVariableName).ToArray().Should().BeEquivalentTo(originalVariableNames, $"because we expected the original variable name to be {String.Join(", ", originalVariableNames)}");
        actual.Select(x => x.Formatters).ToArray().Should().BeEquivalentTo(formatters);
        actual.Select(x => x.DefaultValue).ToArray().Should().BeEquivalentTo(defaultValues, $"because we expected the default value to be {String.Join(", ", defaultValues)}");
    }
}