using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using FluentAssertions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Services;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Modules.GclReplacements.Services;

public class ReplacementsMediatorTests
{
    private const string TestHtmlString = """
                                          <header>
                                              <h1>Product {title:HtmlEncode~Default Product} (#{articleNumber~0000})</h1>
                                          </header>
                                          <article>
                                              <h2>Price</h2>
                                              {price:CurrencySup(true, nl-NL)}
                                              <h2>Description</h2>
                                              {description:Raw~<p>No description available.</p>}
                                              <h2>Specifications</h2>
                                              <ul>
                                                  <li>Weight: {weight:FormatNumber(N2, nl-NL)} kg</li>
                                                  <li>Dimensions: {dimensions:Raw~No dimensions available}</li>
                                                  <li>Color: {color:HtmlEncode~No color available}</li>
                                                  <li>Material: {material~UNAVAILABLE:Uppercase}</li>
                                                  [if({categoryId}=1)]<li>Category: {categoryTitle}</li>[endif]
                                              </ul>
                                          </article>
                                          """;

    private const string TestQueryString = """
                                           SELECT
                                               '{id}' AS originalProductId,
                                               '{id:Raw}' AS rawProductId,
                                               '{categoryId}' AS originalCategoryId,
                                               '{categoryId:Raw}' AS rawCategoryId,
                                               product.id AS productId,
                                               product.title AS productTitle,
                                               category.id AS categoryId,
                                               category.title AS categoryTitle,
                                               CONCAT_WS('', description.value, description.long_value) AS description,
                                               color.value AS color
                                           FROM product_wiser_item AS product
                                           JOIN wiser_itemlink AS productToCategory ON productToCategory.item_id = product.id AND productToCategory.destination_item_id = {categoryId~0} AND productToCategory.type = 100
                                           JOIN product_wiser_item AS category ON category.id = productToCategory.destination_item_id and category.entity_type = 'category'
                                           LEFT JOIN product_wiser_itemdetail AS description ON description.item_id = product.id AND description.key = 'description' AND description.language_code = '{languageCode}'
                                           LEFT JOIN product_wiser_itemdetail AS color ON color.item_id = product.id AND color.key = 'color' AND color.language_code = '{languageCode}'
                                           WHERE product.id = {id~0}
                                           AND product.entity_type = 'product'
                                           AND product.published_environment >= {environment~15}
                                           """;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private IReplacementsMediator replacementsMediator;
    private Mock<IDatabaseConnection> databaseConnectionMock;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    [SetUp]
    public void Setup()
    {
        // Create mocks.
        databaseConnectionMock = new Mock<IDatabaseConnection>();

        // Create the service that we're testing.
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
    [TestCase("CutString(5)", "CutString", new object[] {5, ""})]
    [TestCase("CutString(5,test)", "CutString", new object[] {5, "test"})]
    [TestCase("FormatNumber", "FormatNumber", new object[] {"N2", "nl-NL"})]
    [TestCase("Currency", "Currency", new object[] {true, null})]
    [TestCase("Currency(false)", "Currency", new object[] {false, null})]
    [TestCase("Currency(false, en-US)", "Currency", new object[] {false, "en-US"})]
    [TestCase("CurrencySup", "CurrencySup", new object[] {true, null})]
    [TestCase("CurrencySup(false)", "CurrencySup", new object[] {false, null})]
    [TestCase("CurrencySup(false, en-US)", "CurrencySup", new object[] {false, "en-US"})]
    [TestCase("UppercaseFirst", "UppercaseFirst", null)]
    [TestCase("LowercaseFirst", "LowercaseFirst", null)]
    [TestCase("StripHtml", "StripHtml", null)]
    [TestCase("JsonSafe", "JsonSafe", null)]
    [TestCase("StripInlineStyle", "StripInlineStyle", null)]
    [TestCase("Encrypt", "Encrypt", new object[] {false})]
    [TestCase("Encrypt(true)", "Encrypt", new object[] {true})]
    [TestCase("EncryptNormal", "EncryptNormal", new object[] {false})]
    [TestCase("EncryptNormal(true)", "EncryptNormal", new object[] {true})]
    [TestCase("Decrypt", "Decrypt", new object[] {false, 0})]
    [TestCase("Decrypt(true)", "Decrypt", new object[] {true, 0})]
    [TestCase("Decrypt(true,5)", "Decrypt", new object[] {true, 5})]
    [TestCase("DecryptNormal", "DecryptNormal", new object[] {false, 0})]
    [TestCase("DecryptNormal(true)", "DecryptNormal", new object[] {true, 0})]
    [TestCase("DecryptNormal(true,5)", "DecryptNormal", new object[] {true, 5})]
    [TestCase("Base64", "Base64", null)]
    [TestCase("DateTime(yyyy-MM-dd)", "DateTime", new object[] {"yyyy-MM-dd", null})]
    [TestCase("DateTime(yyyy-MM-dd, nl-NL)", "DateTime", new object[] {"yyyy-MM-dd", "nl-NL"})]
    [TestCase("Sha512", "Sha512", null)]
    [TestCase("Hash(SHA512, Base64)", "Hash", new object[] {"SHA512", "Base64"})]
    [TestCase("Uppercase", "Uppercase", new object[] {false})]
    [TestCase("Uppercase(true)", "Uppercase", new object[] {true})]
    [TestCase("Lowercase", "Lowercase", new object[] {false})]
    [TestCase("Lowercase(true)", "Lowercase", new object[] {true})]
    [TestCase("QrCode(1,1)", "QrCode", new object[] {1, 1})]
    [TestCase("Replace(1, 2)", "Replace",new object[] {"1", "2"})]
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
    [TestCase("String with {valid} variable", "{valid}", "valid", "valid", new[] {"HtmlEncode"}, "")]
    [TestCase("String with {Valid:Hash(SHA512, Base64)} variable", "{Valid:Hash(SHA512, Base64)}", "Valid", "Valid:Hash(SHA512, Base64)", new[] {"HtmlEncode", "Hash(SHA512, Base64)"}, "")]
    [TestCase("String with {valid:Raw} variable", "{valid:Raw}", "valid", "valid:Raw", new[] {"Raw"}, "")]
    [TestCase("String with {valid~DefaultValue} variable", "{valid~DefaultValue}", "valid", "valid~DefaultValue", new[] {"HtmlEncode"}, "DefaultValue")]
    [TestCase("String with {valid~DefaultValue:Raw} variable", "{valid~DefaultValue:Raw}", "valid", "valid~DefaultValue:Raw", new[] {"Raw"}, "DefaultValue")]
    [TestCase("String with {valid:Raw~DefaultValue} variable", "{valid:Raw~DefaultValue}", "valid", "valid:Raw~DefaultValue", new[] {"Raw"}, "DefaultValue")]
    [TestCase("String with {Valid~DefaultValue:Hash(SHA512, Base64)} variable", "{Valid~DefaultValue:Hash(SHA512, Base64)}", "Valid", "Valid~DefaultValue:Hash(SHA512, Base64)", new[] {"HtmlEncode", "Hash(SHA512, Base64)"}, "DefaultValue")]
    [TestCase("String with {Valid:Hash(SHA512, Base64)~DefaultValue} variable", "{Valid:Hash(SHA512, Base64)~DefaultValue}", "Valid", "Valid:Hash(SHA512, Base64)~DefaultValue", new[] {"HtmlEncode", "Hash(SHA512, Base64)"}, "DefaultValue")]
    [TestCase("String with {valid:Seo} variable", "{valid:Seo}", "valid", "valid:Seo", new[] {"HtmlEncode", "Seo"}, "")]
    [TestCase("String with {valid:HtmlEncode} variable", "{valid:HtmlEncode}", "valid", "valid:HtmlEncode", new[] {"HtmlEncode"}, "")]
    [TestCase("String with {valid:UrlEncode} variable", "{valid:UrlEncode}", "valid", "valid:UrlEncode", new[] {"UrlEncode"}, "")]
    [TestCase("String with {valid:UrlDecode} variable", "{valid:UrlDecode}", "valid", "valid:UrlDecode", new[] {"HtmlEncode", "UrlDecode"}, "")]
    [TestCase("String with {valid:CutString(10, test)} variable", "{valid:CutString(10, test)}", "valid", "valid:CutString(10, test)", new[] {"HtmlEncode", "CutString(10, test)"}, "")]
    [TestCase("String with {valid:FormatNumber(C, en-UK)} variable", "{valid:FormatNumber(C, en-UK)}", "valid", "valid:FormatNumber(C, en-UK)", new[] {"HtmlEncode", "FormatNumber(C, en-UK)"}, "")]
    [TestCase("String with {valid:Currency(false, en-UK)} variable", "{valid:Currency(false, en-UK)}", "valid", "valid:Currency(false, en-UK)", new[] {"HtmlEncode", "Currency(false, en-UK)"}, "")]
    [TestCase("String with {valid:CurrencySup(false, en-UK)} variable", "{valid:CurrencySup(false, en-UK)}", "valid", "valid:CurrencySup(false, en-UK)", new[] {"CurrencySup(false, en-UK)"}, "")]
    [TestCase("String with {valid:UppercaseFirst} variable", "{valid:UppercaseFirst}", "valid", "valid:UppercaseFirst", new[] {"HtmlEncode", "UppercaseFirst"}, "")]
    [TestCase("String with {valid:LowercaseFirst} variable", "{valid:LowercaseFirst}", "valid", "valid:LowercaseFirst", new[] {"HtmlEncode", "LowercaseFirst"}, "")]
    [TestCase("String with {valid:StripHtml} variable", "{valid:StripHtml}", "valid", "valid:StripHtml", new[] {"HtmlEncode", "StripHtml"}, "")]
    [TestCase("String with {valid:JsonSafe} variable", "{valid:JsonSafe}", "valid", "valid:JsonSafe", new[] {"HtmlEncode", "JsonSafe"}, "")]
    [TestCase("String with {valid:StripInlineStyle} variable", "{valid:StripInlineStyle}", "valid", "valid:StripInlineStyle", new[] {"HtmlEncode", "StripInlineStyle"}, "")]
    [TestCase("String with {valid:Encrypt(true)} variable", "{valid:Encrypt(true)}", "valid", "valid:Encrypt(true)", new[] {"HtmlEncode", "Encrypt(true)"}, "")]
    [TestCase("String with {valid:EncryptNormal(true)} variable", "{valid:EncryptNormal(true)}", "valid", "valid:EncryptNormal(true)", new[] {"HtmlEncode", "EncryptNormal(true)"}, "")]
    [TestCase("String with {valid:Decrypt(true)} variable", "{valid:Decrypt(true)}", "valid", "valid:Decrypt(true)", new[] {"HtmlEncode", "Decrypt(true)"}, "")]
    [TestCase("String with {valid:DecryptNormal(true)} variable", "{valid:DecryptNormal(true)}", "valid", "valid:DecryptNormal(true)", new[] {"HtmlEncode", "DecryptNormal(true)"}, "")]
    [TestCase("String with {valid:Base64} variable", "{valid:Base64}", "valid", "valid:Base64", new[] {"HtmlEncode", "Base64"}, "")]
    [TestCase("String with {valid:DateTime(yyyy-MM-dd, nl-NL)} variable", "{valid:DateTime(yyyy-MM-dd, nl-NL)}", "valid", "valid:DateTime(yyyy-MM-dd, nl-NL)", new[] {"HtmlEncode", "DateTime(yyyy-MM-dd, nl-NL)"}, "")]
    [TestCase("String with {valid:Sha512} variable", "{valid:Sha512}", "valid", "valid:Sha512", new[] {"HtmlEncode", "Sha512"}, "")]
    [TestCase("String with {valid:Hash(SHA512, Base64)} variable", "{valid:Hash(SHA512, Base64)}", "valid", "valid:Hash(SHA512, Base64)", new[] {"HtmlEncode", "Hash(SHA512, Base64)"}, "")]
    [TestCase("String with {valid:Uppercase(true)} variable", "{valid:Uppercase(true)}", "valid", "valid:Uppercase(true)", new[] {"HtmlEncode", "Uppercase(true)"}, "")]
    [TestCase("String with {valid:Lowercase(true)} variable", "{valid:Lowercase(true)}", "valid", "valid:Lowercase(true)", new[] {"HtmlEncode", "Lowercase(true)"}, "")]
    [TestCase("String with {valid:QrCode(100, 100)} variable", "{valid:QrCode(100, 100)}", "valid", "valid:QrCode(100, 100)", new[] {"HtmlEncode", "QrCode(100, 100)"}, "")]
    [TestCase("String with {valid:Replace(1, 2)} variable", "{valid:Replace(1, 2)}", "valid", "valid:Replace(1, 2)", new[] {"HtmlEncode", "Replace(1, 2)"}, "")]
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
    [TestCase("This is a {test} string with {multiple} variables.", new[] {"{test}", "{multiple}"}, new[] {"test", "multiple"}, new[] {"test", "multiple"}, new[] {"", ""}, new[] {"HtmlEncode"}, new[] {"HtmlEncode"})]
    [TestCase("This is a {test:Raw} string with {multiple:Uppercase} variables.", new[] {"{test:Raw}", "{multiple:Uppercase}"}, new[] {"test", "multiple"}, new[] {"test:Raw", "multiple:Uppercase"}, new[] {"", ""}, new[] {"Raw"}, new[] {"HtmlEncode", "Uppercase"})]
    [TestCase("This is a {test~DefaultValue1} string with {multiple:Uppercase~DefaultValue2} {variables~DefaultValue3:Lowercase}.", new[] {"{test~DefaultValue1}", "{multiple:Uppercase~DefaultValue2}", "{variables~DefaultValue3:Lowercase}"}, new[] {"test", "multiple", "variables"}, new[] {"test~DefaultValue1", "multiple:Uppercase~DefaultValue2", "variables~DefaultValue3:Lowercase"}, new[] {"DefaultValue1", "DefaultValue2", "DefaultValue3"}, new[] {"HtmlEncode"}, new[] {"HtmlEncode", "Uppercase"}, new[] {"HtmlEncode", "Lowercase"})]
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

    [Test]
    [TestCase("")]
    [TestCase("This is a string without any variables.")]
    public void RemoveTemplateVariables_StringsWithoutVariables_ReturnsInputString(string input)
    {
        // Act
        var actual = replacementsMediator.RemoveTemplateVariables(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any replacement variables");
    }

    [Test]
    [TestCase("This is a {test} string with a single variable.", "This is a  string with a single variable.")]
    [TestCase("This is a {test} string with {multiple} variables.", "This is a  string with  variables.")]
    [TestCase("""
              This is a {test} string with a {lot} variables.
              <div>
                  <h1>Title: {title}</h1>
                  <p>And some html tags. {variable1}, {variable2}</p>
              </div>
              """,
        """
        This is a  string with a  variables.
        <div>
            <h1>Title: </h1>
            <p>And some html tags. , </p>
        </div>
        """)]
    public void RemoveTemplateVariables_StringsWithVariables_ReturnsStringWithoutVariables(string input, string expected)
    {
        // Act
        var actual = replacementsMediator.RemoveTemplateVariables(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing a string that contains replacement variables");
    }

    [Test]
    [TestCase("")]
    [TestCase("This is a string without any variables.")]
    public void HandleVariablesDefaultValues_StringsWithoutVariables_ReturnsInputString(string input)
    {
        // Act
        var actual = replacementsMediator.HandleVariablesDefaultValues(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any replacement variables");
    }

    [Test]
    [TestCase("This is a {test} string with a single variable.")]
    [TestCase("This is a {test} string with {multiple} variables.")]
    [TestCase("""
              This is a {test} string with a {lot} variables.
              <div>
                  <h1>Title: {title}</h1>
                  <p>And some html tags. {variable1}, {variable2}</p>
              </div>
              """)]
    public void HandleVariablesDefaultValues_StringsWithVariablesWithoutDefaultValues_ReturnsStringWithVariables(string input)
    {
        // Act
        var actual = replacementsMediator.HandleVariablesDefaultValues(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing a string that contains replacement variables without default values");
    }

    [Test]
    [TestCase("This is a {test~DefaultValue1} string with a single variable.", "This is a DefaultValue1 string with a single variable.")]
    [TestCase("This is a {test~DefaultValue1} string with {multiple~DefaultValue2} variables.", "This is a DefaultValue1 string with DefaultValue2 variables.")]
    [TestCase("""
              This is a {test~DefaultValue1} string with a {lot~DefaultValue2} variables.
              <div>
                  <h1>Title: {title~DefaultValue3}</h1>
                  <p>And some html tags. {variable1~DefaultValue4}, {variable2~DefaultValue5}</p>
              </div>
              """,
        """
        This is a DefaultValue1 string with a DefaultValue2 variables.
        <div>
            <h1>Title: DefaultValue3</h1>
            <p>And some html tags. DefaultValue4, DefaultValue5</p>
        </div>
        """)]
    public void HandleVariablesDefaultValues_StringsWithVariablesWithDefaultValues_ReturnsStringWithDefaultValues(string input, string expected)
    {
        // Act
        var actual = replacementsMediator.HandleVariablesDefaultValues(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing a string that contain variables with default values");
    }

    [Test]
    [TestCase("")]
    [TestCase("This is a string without any if statements.")]
    public void EvaluateTemplate_StringWithoutIfStatement_ReturnsOriginalString(string input)
    {
        // Act
        var actual = replacementsMediator.EvaluateTemplate(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCase("This is a string with an invalid if statement: [if(1-1)]It's true[else]It's false[endif].")]
    [TestCase("This is a string with an invalid if statement: [if(1=a]It's true[else]It's false[endif].")]
    [TestCase("This is a string with an invalid if statement: [if(1=a)]It's true[ele]It's false[end].")]
    [TestCase("This is a string with an invalid if statement: [if(1=a)]It's true[else]It's false[endif.")]
    [TestCase("This is a string with an invalid if statement: [if1=a)]It's true[ele]It's false[endif.")]
    public void EvaluateTemplate_StringWithInvalidIfStatement_ReturnsOriginalString(string input)
    {
        // Act
        var actual = replacementsMediator.EvaluateTemplate(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCase("This is a string with a single if statement that should return true: [if(1=1)]It's true[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(1=1)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(=)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(2>1)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(1<2)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(abcde%ab)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return true: [if(a!b)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return true: It's true.")]
    [TestCase("This is a string with a single if statement that should return false: [if(1=2)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return false: [if(1=)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return false: [if(2>3)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return false: [if(3<2)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return false: [if(abcde%xyz)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return false: [if(a!a)]It's true[else]It's false[endif].", "This is a string with a single if statement that should return false: It's false.")]
    [TestCase("This is a string with a single if statement that should return empty: [if(1=2)]It's true[endif].", "This is a string with a single if statement that should return empty: .")]
    public void EvaluateTemplate_StringWithSingleIfStatements_ReturnsParsedStrings(string input, string expected)
    {
        // Act
        var actual = replacementsMediator.EvaluateTemplate(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCase("This is a string with multiple if statements: [if(1=1)]It's true[else]It's false[endif]. And the second: [if(test=test)]It's true[else]It's false[endif].", "This is a string with multiple if statements: It's true. And the second: It's true.")]
    [TestCase("This is a string with multiple if statements: [if(1=1)]It's true [if(x=x)]and also true[else]and false[endif][else]It's false[endif]. And the second: [if(test=test)]It's true [if(1>0)]and also true[else]and false[endif][else]It's false[endif].", "This is a string with multiple if statements: It's true and also true. And the second: It's true and also true.")]
    [TestCase("This is a string with multiple if statements: [if(2<10)]It's true [if(2>10)]and also true[else]and false[endif][else]It's false[endif]. And the second: [if(10>9)]It's true [if(15>12)]and also true[else]and false[endif][else]It's false[endif].", "This is a string with multiple if statements: It's true and false. And the second: It's true and also true.")]
    [TestCase("This is a string with multiple if statements: [if(2>11)]It's true [if(2>10)]and also true[else]and false[endif][else]It's false [if(21>10)]and true[else]and also false[endif][endif]. And the second: [if(10>11)]It's true [if(15>12)]and also true[else]and false[endif][else]It's false [if(test1=test2)]and true[else]and also false[endif][endif].", "This is a string with multiple if statements: It's false and true. And the second: It's false and also false.")]
    public void EvaluateTemplate_StringWithMultipleIfStatements_ReturnsParsedStrings(string input, string expected)
    {
        // Act
        var actual = replacementsMediator.EvaluateTemplate(input);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCase("")]
    [TestCase("This is a string without any if statements.")]
    public void DoReplacements_StringWithoutReplacements_ReturnsOriginalString(string input)
    {
        // Act
        var actual = replacementsMediator.DoReplacements(input, new Dictionary<string, string>());

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(input, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCaseSource(nameof(GetTestCasesForDoReplacementsWithDictionary))]
    public void DoReplacements_StringWithReplacementsDictionary_ReturnsParsedString(string input, IDictionary<string, object> replaceData, string expected, bool forQuery, string defaultFormatter)
    {
        // Act
        var actual = replacementsMediator.DoReplacements(input: input, replaceData: replaceData, prefix: "{", suffix: "}", forQuery: forQuery, defaultFormatter: defaultFormatter);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCaseSource(nameof(GetTestCasesForDoReplacementsWithJObject))]
    public void DoReplacements_StringWithReplacementsJObject_ReturnsParsedString(string input, JObject replaceData, string expected, bool forQuery, string defaultFormatter)
    {
        // Act
        var actual = replacementsMediator.DoReplacements(input: input, replaceData: replaceData, prefix: "{", suffix: "}", forQuery: forQuery, defaultFormatter: defaultFormatter);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCaseSource(nameof(GetTestCasesForDoReplacementsWithJArray))]
    public void DoReplacements_StringWithReplacementsJArray_ReturnsParsedString(string input, JArray replaceData, string expected, bool forQuery, string defaultFormatter)
    {
        // Act
        var actual = replacementsMediator.DoReplacements(input: input, replaceData: replaceData, prefix: "{", suffix: "}", forQuery: forQuery, defaultFormatter: defaultFormatter);

        // Assert
        actual.Should().NotBeNull("because we're testing specific strings that should always return a value");
        actual.Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    [TestCaseSource(nameof(GetTestCasesForDoReplacementsWithDataSet))]
    public void DoReplacements_StringWithReplacementsDataSet_ReturnsParsedString(string input, DataSet replaceData, string expected, bool forQuery, string defaultFormatter)
    {
        // Act
        var actual = replacementsMediator.DoReplacements(input: input, replaceData: replaceData, prefix: "{", suffix: "}", forQuery: forQuery, defaultFormatter: defaultFormatter);

        // Assert
        var enumerable = actual.ToList();
        enumerable.Should().NotBeNull("because we're testing specific strings that should always return a value");
        enumerable.Should().HaveCount(1, "because we're testing a DataSet with only one table");
        enumerable.Single().Should().HaveCount(1, "because we're testing a DataSet with only one table and one row");
        enumerable.First().First().Should().Be(expected, "because we're testing strings that don't contain any if statements");
    }

    [Test]
    public void DoReplacements_MultipleStringsWithReplacementsDataSet_ReturnsParsedStrings()
    {
        // Arrange
        var input = "This is a string with a single variable: {test}.";

        var replaceData = new DataSet();
        replaceData.Tables.Add(new DataTable());
        replaceData.Tables[0].Columns.Add("test", typeof(string));
        replaceData.Tables[0].Rows.Add("Value1");
        replaceData.Tables[0].Rows.Add("Value2");
        replaceData.Tables.Add(new DataTable());
        replaceData.Tables[1].Columns.Add("test", typeof(string));
        replaceData.Tables[1].Rows.Add("Value3");
        replaceData.Tables[1].Rows.Add("Value4");

        var expected = new List<List<string>>
        {
            new List<string>
            {
                "This is a string with a single variable: Value1.",
                "This is a string with a single variable: Value2."
            },
            new List<string>
            {
                "This is a string with a single variable: Value3.",
                "This is a string with a single variable: Value4."
            }
        };

        // Act
        var actual = replacementsMediator.DoReplacements(input: input, replaceData: replaceData, prefix: "{", suffix: "}", forQuery: false, defaultFormatter: "HtmlEncode");

        // Assert
        var enumerable = actual.Select(x => x.ToList()).ToList();
        enumerable.Should().NotBeNull("because we're testing specific strings that should always return a value");
        enumerable.Should().BeEquivalentTo(expected, "because we're testing strings that don't contain any if statements");
    }

    /// <summary>
    /// Function to get test cases for the DoReplacements method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <returns>The values for the parameters of the test methods for DoReplacements.</returns>
    private static IEnumerable<object[]> GetTestCasesForDoReplacementsWithDictionary()
    {
        return GetTestCasesForDoReplacements(new List<object> {
            new Dictionary<string, object>(),
            new Dictionary<string, object>(),
            new Dictionary<string, object> {{"test1", "Value1"}},
            new Dictionary<string, object> {{"test1", "Value1"}, {"test2", "Value2"}},
            new Dictionary<string, object>
            {
                {"title", "T-Shirt Blue"},
                {"articleNumber", "1337"},
                {"price", 19.99},
                {"description", "<p>Short description</p>"},
                {"weight", 0.5},
                {"dimensions", "XL"},
                {"color", "Blue"},
                {"material", "Cotton"},
                {"categoryId", 1},
                {"categoryTitle", "T-shirts"}
            },

            new Dictionary<string, object>
            {
                {"title", "T-Shirt Green"},
                {"price", 15.5},
                {"weight", 0.5},
                {"dimensions", "L"},
                {"color", "Green"},
                {"material", null},
                {"categoryId", 1},
                {"categoryTitle", "T-shirts"}
            },

            new Dictionary<string, object>
            {
                {"id", 1},
                {"languageCode", "en-US"},
                {"environment", 15},
                {"categoryId", 1}
            },

            new Dictionary<string, object>
            {
                {"id", 1}
            },

            new Dictionary<string, object>
            {
                {"id", 1},
                {"languageCode", null},
                {"environment", null},
                {"categoryId", null}
            }
        });
    }

    /// <summary>
    /// Function to get test cases for the DoReplacements method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <returns>The values for the parameters of the test methods for DoReplacements.</returns>
    private static IEnumerable<object[]> GetTestCasesForDoReplacementsWithJObject()
    {
        return GetTestCasesForDoReplacements(new List<object> {
            new JObject(),
            new JObject(),
            new JObject {{"test1", "Value1"}},
            new JObject {{"test1", "Value1"}, {"test2", "Value2"}},
            new JObject
            {
                {"title", "T-Shirt Blue"},
                {"articleNumber", "1337"},
                {"price", 19.99},
                {"description", "<p>Short description</p>"},
                {"weight", 0.5},
                {"dimensions", "XL"},
                {"color", "Blue"},
                {"material", "Cotton"},
                {"categoryId", 1},
                {"categoryTitle", "T-shirts"}
            },

            new JObject
            {
                {"title", "T-Shirt Green"},
                {"price", 15.5},
                {"weight", 0.5},
                {"dimensions", "L"},
                {"color", "Green"},
                {"material", null},
                {"categoryId", 1},
                {"categoryTitle", "T-shirts"}
            },

            new JObject
            {
                {"id", 1},
                {"languageCode", "en-US"},
                {"environment", 15},
                {"categoryId", 1}
            },

            new JObject
            {
                {"id", 1}
            },

            new JObject
            {
                {"id", 1},
                {"languageCode", null},
                {"environment", null},
                {"categoryId", null}
            }
        });
    }

    /// <summary>
    /// Function to get test cases for the DoReplacements method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <returns>The values for the parameters of the test methods for DoReplacements.</returns>
    private static IEnumerable<object[]> GetTestCasesForDoReplacementsWithJArray()
    {
        return GetTestCasesForDoReplacements(new List<object> {
            new JArray(),
            new JArray {new JObject()},
            new JArray {new JObject {new JProperty("test1", "Value1")}},
            new JArray {new JObject {{"test1", "Value1"}}, new JObject {{"test2", "Value2"}}},
            new JArray
            {
                new JObject
                {
                    {"title", "T-Shirt Blue"},
                    {"articleNumber", "1337"},
                    {"price", 19.99},
                    {"description", "<p>Short description</p>"},
                    {"weight", 0.5},
                    {"dimensions", "XL"},
                    {"color", "Blue"},
                    {"material", "Cotton"},
                    {"categoryId", 1},
                    {"categoryTitle", "T-shirts"}
                }
            },

            new JArray
            {
                new JObject
                {
                    {"title", "T-Shirt Green"},
                    {"price", 15.5},
                    {"weight", 0.5},
                    {"dimensions", "L"},
                    {"color", "Green"},
                    {"material", null},
                    {"categoryId", 1},
                    {"categoryTitle", "T-shirts"}
                }
            },

            new JArray
            {
                new JObject
                {
                    {"id", 1},
                    {"languageCode", "en-US"},
                    {"environment", 15},
                    {"categoryId", 1}
                }
            },

            new JArray
            {
                new JObject
                {
                    {"id", 1}
                }
            },

            new JArray
            {
                new JObject
                {
                    {"id", 1},
                    {"languageCode", null},
                    {"environment", null},
                    {"categoryId", null}
                }
            }
        });
    }

    /// <summary>
    /// Function to get test cases for the DoReplacements method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <returns>The values for the parameters of the test methods for DoReplacements.</returns>
    private static IEnumerable<object[]> GetTestCasesForDoReplacementsWithDataSet()
    {
        var data = new List<object>();

        // Test 1.
        var dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("x", typeof(string))
        });
        dataTable.Rows.Add("y");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 2.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("test0", typeof(string))
        });
        dataTable.Rows.Add("test0");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 3.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("test1", typeof(string))
        });
        dataTable.Rows.Add("Value1");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 4.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("test1", typeof(string)),
            new DataColumn("test2", typeof(string))
        });
        dataTable.Rows.Add("Value1", "Value2");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 5.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("title", typeof(string)),
            new DataColumn("articleNumber", typeof(string)),
            new DataColumn("price", typeof(decimal)),
            new DataColumn("description", typeof(string)),
            new DataColumn("weight", typeof(decimal)),
            new DataColumn("dimensions", typeof(string)),
            new DataColumn("color", typeof(string)),
            new DataColumn("material", typeof(string)),
            new DataColumn("categoryId", typeof(ulong)),
            new DataColumn("categoryTitle", typeof(string)),
        });
        dataTable.Rows.Add("T-Shirt Blue", "1337", 19.99, "<p>Short description</p>", 0.5, "XL", "Blue", "Cotton", 1, "T-shirts");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 6.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("title", typeof(string)),
            new DataColumn("price", typeof(decimal)),
            new DataColumn("weight", typeof(decimal)),
            new DataColumn("dimensions", typeof(string)),
            new DataColumn("color", typeof(string)),
            new DataColumn("material", typeof(string)),
            new DataColumn("categoryId", typeof(ulong)),
            new DataColumn("categoryTitle", typeof(string)),
        });
        dataTable.Rows.Add("T-Shirt Green", 15.5, 0.5, "L", "Green", null, 1, "T-shirts");
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 7.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("id", typeof(ulong)),
            new DataColumn("languageCode", typeof(string)),
            new DataColumn("environment", typeof(int)),
            new DataColumn("categoryId", typeof(ulong)),
        });
        dataTable.Rows.Add(1, "en-US", 15, 1);
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 8.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("id", typeof(ulong)),
        });
        dataTable.Rows.Add(1);
        data.Add(new DataSet {Tables = {dataTable}});

        // Test 9.
        dataTable = new DataTable();
        dataTable.Columns.AddRange(new DataColumn[] {
            new DataColumn("id", typeof(ulong)),
            new DataColumn("languageCode", typeof(string)),
            new DataColumn("environment", typeof(int)),
            new DataColumn("categoryId", typeof(ulong)),
        });
        dataTable.Rows.Add(1, null, null, null);
        data.Add(new DataSet {Tables = {dataTable}});

        return GetTestCasesForDoReplacements(data);
    }

    /// <summary>
    /// Function to get test cases for the DoReplacements method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <param name="replacementData">A list of 9 items, each item in the list is an object (Dictionary, JToken, DataRow etc) to use as the replacement data for the different overloads of the DoReplacements method.</param>
    /// <returns>The values for the parameters of the test methods for DoReplacements.</returns>
    private static IEnumerable<object[]> GetTestCasesForDoReplacements(List<object> replacementData)
    {
        return new object[][]
        {
            // Simple strings with one variable.
            new object[] {
                "This is a {test1} string with a single variable.",
                replacementData[0],
                "This is a {test1} string with a single variable.",
                false,
                "HtmlEncode"
            },
            new object[] {
                "This is a {test1~DefaultValue} string with a single variable.",
                replacementData[1],
                "This is a {test1~DefaultValue} string with a single variable.",
                false,
                "HtmlEncode"
            },
            new object[] {
                "This is a {test1} string with a single variable.",
                replacementData[2],
                "This is a Value1 string with a single variable.",
                false,
                "HtmlEncode"
            },
            new object[] {
                "This is a {test1~DefaultValue1} string with a single variable.",
                replacementData[3],
                "This is a Value1 string with a single variable.",
                false,
                "HtmlEncode"
            },

            // Long HTML strings with multiple variables, default values and formatters.
            new object[] {
                TestHtmlString,
                replacementData[4],
                """
                <header>
                    <h1>Product T-Shirt Blue (#1337)</h1>
                </header>
                <article>
                    <h2>Price</h2>
                    € 19,<sup>99</sup>
                    <h2>Description</h2>
                    <p>Short description</p>
                    <h2>Specifications</h2>
                    <ul>
                        <li>Weight: 0,50 kg</li>
                        <li>Dimensions: XL</li>
                        <li>Color: Blue</li>
                        <li>Material: COTTON</li>
                        [if(1=1)]<li>Category: T-shirts</li>[endif]
                    </ul>
                </article>
                """,
                false,
                "HtmlEncode"
            },
            new object[] {
                TestHtmlString,
                replacementData[5],
                """
                <header>
                    <h1>Product T-Shirt Green (#{articleNumber~0000})</h1>
                </header>
                <article>
                    <h2>Price</h2>
                    € 15,<sup>50</sup>
                    <h2>Description</h2>
                    {description:Raw~<p>No description available.</p>}
                    <h2>Specifications</h2>
                    <ul>
                        <li>Weight: 0,50 kg</li>
                        <li>Dimensions: L</li>
                        <li>Color: Green</li>
                        <li>Material: UNAVAILABLE</li>
                        [if(1=1)]<li>Category: T-shirts</li>[endif]
                    </ul>
                </article>
                """,
                false,
                "HtmlEncode"
            },

            // Queries with multiple variables, default values and formatters.
            new object[] {
                TestQueryString,
                replacementData[6],
                """
                SELECT
                    ?sql_id AS originalProductId,
                    '1' AS rawProductId,
                    ?sql_categoryId AS originalCategoryId,
                    '1' AS rawCategoryId,
                    product.id AS productId,
                    product.title AS productTitle,
                    category.id AS categoryId,
                    category.title AS categoryTitle,
                    CONCAT_WS('', description.value, description.long_value) AS description,
                    color.value AS color
                FROM product_wiser_item AS product
                JOIN wiser_itemlink AS productToCategory ON productToCategory.item_id = product.id AND productToCategory.destination_item_id = ?sql_categoryId AND productToCategory.type = 100
                JOIN product_wiser_item AS category ON category.id = productToCategory.destination_item_id and category.entity_type = 'category'
                LEFT JOIN product_wiser_itemdetail AS description ON description.item_id = product.id AND description.key = 'description' AND description.language_code = ?sql_languageCode
                LEFT JOIN product_wiser_itemdetail AS color ON color.item_id = product.id AND color.key = 'color' AND color.language_code = ?sql_languageCode
                WHERE product.id = ?sql_id
                AND product.entity_type = 'product'
                AND product.published_environment >= ?sql_environment
                """,
                true,
                "HtmlEncode"
            },

            // Queries with multiple variables, default values and formatters.
            new object[] {
                TestQueryString,
                replacementData[7],
                """
                SELECT
                    ?sql_id AS originalProductId,
                    '1' AS rawProductId,
                    '{categoryId}' AS originalCategoryId,
                    '{categoryId:Raw}' AS rawCategoryId,
                    product.id AS productId,
                    product.title AS productTitle,
                    category.id AS categoryId,
                    category.title AS categoryTitle,
                    CONCAT_WS('', description.value, description.long_value) AS description,
                    color.value AS color
                FROM product_wiser_item AS product
                JOIN wiser_itemlink AS productToCategory ON productToCategory.item_id = product.id AND productToCategory.destination_item_id = {categoryId~0} AND productToCategory.type = 100
                JOIN product_wiser_item AS category ON category.id = productToCategory.destination_item_id and category.entity_type = 'category'
                LEFT JOIN product_wiser_itemdetail AS description ON description.item_id = product.id AND description.key = 'description' AND description.language_code = '{languageCode}'
                LEFT JOIN product_wiser_itemdetail AS color ON color.item_id = product.id AND color.key = 'color' AND color.language_code = '{languageCode}'
                WHERE product.id = ?sql_id
                AND product.entity_type = 'product'
                AND product.published_environment >= {environment~15}
                """,
                true,
                "HtmlEncode"
            },

            // Queries with multiple variables, default values and formatters.
            new object[] {
                TestQueryString,
                replacementData[8],
                """
                SELECT
                    ?sql_id AS originalProductId,
                    '1' AS rawProductId,
                    '' AS originalCategoryId,
                    '' AS rawCategoryId,
                    product.id AS productId,
                    product.title AS productTitle,
                    category.id AS categoryId,
                    category.title AS categoryTitle,
                    CONCAT_WS('', description.value, description.long_value) AS description,
                    color.value AS color
                FROM product_wiser_item AS product
                JOIN wiser_itemlink AS productToCategory ON productToCategory.item_id = product.id AND productToCategory.destination_item_id = 0 AND productToCategory.type = 100
                JOIN product_wiser_item AS category ON category.id = productToCategory.destination_item_id and category.entity_type = 'category'
                LEFT JOIN product_wiser_itemdetail AS description ON description.item_id = product.id AND description.key = 'description' AND description.language_code = ''
                LEFT JOIN product_wiser_itemdetail AS color ON color.item_id = product.id AND color.key = 'color' AND color.language_code = ''
                WHERE product.id = ?sql_id
                AND product.entity_type = 'product'
                AND product.published_environment >= 15
                """,
                true,
                "HtmlEncode"
            }
        };
    }
}