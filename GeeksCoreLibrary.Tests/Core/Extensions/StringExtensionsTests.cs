using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Core.Extensions;

public class StringExtensionsTests
{
    private const string TestEncryptionKey = "Test123";
    private const string TestOtherEncryptionKey = "Encrypted123";
    private const string TestEncryptionSalt = "Test456";
    private const string TestValue = "This is a test value";

    private GclSettings gclSettings;

    [SetUp]
    public void Setup()
    {
        // Create mocks.
        gclSettings = new GclSettings
        {
            DefaultEncryptionSalt = TestEncryptionSalt
        };
    }

    [Test]
    [TestCase("", "a", "b", "")]
    [TestCase(TestValue, "value", "string", "This is a test string")]
    [TestCase("This is a TEST Value", "value", "string", "This is a TEST string")]
    [TestCase("This is a test VALUE", "value", "String", "This is a test String")]
    [TestCase("This is a test value", "VALUE", "string", "This is a test string")]
    public void ReplaceCaseInsensitive_DifferentStrings_ReturnsReplacedString(string input, string oldValue, string newValue, string expected)
    {
        // Act
        var actual = input.ReplaceCaseInsensitive(oldValue, newValue);

        // Assert
        actual.Should().Be(expected, $"because we expected the output to be {expected}");
    }

    [Test]
    [TestCase("", "")]
    [TestCase(TestValue, "this-is-a-test-value")]
    [TestCase("This is a TEST Value", "this-is-a-test-value")]
    [TestCase("This is a test VALUE", "this-is-a-test-value")]
    [TestCase("Thiß is á_tèst & vælue ðøþđłœ 123", "thiss-is-a_test-vaelue-dothdloe-123")]
    public void ConvertToSeo_DifferentStrings_ReturnsSeoUrlString(string input, string expected)
    {
        // Act
        var actual = input.ConvertToSeo();

        // Assert
        actual.Should().Be(expected, $"because we expected the output to be {expected}");
    }

    [Test]
    [TestCase("", false, "")]
    [TestCase("", true, "''")]
    [TestCase(TestValue, false, TestValue)]
    [TestCase(TestValue, true, $"'{TestValue}'")]
    [TestCase("Thiß is á_tèst & vælue ðøþđłœ 123", false, "Thiß is á_tèst & vælue ðøþđłœ 123")]
    [TestCase("Thiß is á_tèst & vælue ðøþđłœ 123", true, "'Thiß is á_tèst & vælue ðøþđłœ 123'")]
    [TestCase("This is a test value'; SELECT * FROM wiser_item;'", false, @"This is a test value\'; SELECT * FROM wiser_item;\'")]
    [TestCase("This is a test value'; SELECT * FROM wiser_item;'", true, @"'This is a test value\'; SELECT * FROM wiser_item;\''")]
    public void ToMySqlSafeValue_DifferentStrings_ReturnsSafeSqlValue(string input, bool encloseInQuotes, string expected)
    {
        // Act
        var actual = input.ToMySqlSafeValue(encloseInQuotes);

        // Assert
        actual.Should().Be(expected, $"because we expected the output to be {expected}");
    }

    [Test]
    [TestCase(TestValue, TestEncryptionKey, false, false)]
    [TestCase(TestValue, TestEncryptionKey, false, true)]
    [TestCase(TestValue, TestEncryptionKey, true, false)]
    [TestCase(TestValue, TestEncryptionKey, true, true)]
    public void EncryptWithAes_DifferentStrings_ReturnsValueDifferentFromInput(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var actual = input.EncryptWithAes(key, withDateTime, useSlowerButMoreSecureMethod);

        // Assert
        actual.Should().NotBeNullOrEmpty("because we expect an encrypted string");
        actual.Should().NotBe(input, "because we expect the encrypted string to be different from the input");
    }

    [Test]
    [TestCase(TestValue, TestEncryptionKey, false, false)]
    [TestCase(TestValue, TestEncryptionKey, false, true)]
    [TestCase("dRZm0Nx9rlnZa42M6tiH4iH3byZWPRzFtQY2zUN8oME=", TestOtherEncryptionKey, false, false)]
    [TestCase("AQH70KbmzKIku8AbSPReKR2GVxSn53UImI7LdbmsiG+3KRWS6xNn5Caig7pYv2llNVLxUF8TtFrVbTU6ZHonPqguIY977qBVnRBfr/FBf5sMLA==", TestOtherEncryptionKey, false, true)]
    [TestCase("dRZm0Nx9rlnZa42M6tiH4syBx/jWPgdyueo2fzFr400wTq+hn6GmRhCKTE4so0sD", TestEncryptionKey, true, false)]
    [TestCase("AQGJZim65rM+B45SIdkYPTWNRl9pZyKI6Aj1rIb/fQ27HcLTiu+U/vcEY2ttgpJPo20wUBP2BSC0fKOf4pI6yQ/TQB0nI4iJxvq/Pn5u5Keq23H2YqLDVwbakJ9I2/+Z0uo=", TestEncryptionKey, true, true)]
    public void DecryptWithAes_InvalidStrings_ThrowsException(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var comparison = () => { input.DecryptWithAes(key, withDateTime, 1, useSlowerButMoreSecureMethod); };

        // Assert
        comparison.Should().Throw<Exception>("because we expect an exception to be thrown when trying to decrypt an invalid string");
    }

    [Test]
    [TestCase("dRZm0Nx9rlnZa42M6tiH4iH3byZWPRzFtQY2zUN8oME=", TestEncryptionKey, false, false)]
    [TestCase("AQH70KbmzKIku8AbSPReKR2GVxSn53UImI7LdbmsiG+3KRWS6xNn5Caig7pYv2llNVLxUF8TtFrVbTU6ZHonPqguIY977qBVnRBfr/FBf5sMLA==", TestEncryptionKey, false, true)]
    [TestCase("dRZm0Nx9rlnZa42M6tiH4syBx/jWPgdyueo2fzFr400wTq+hn6GmRhCKTE4so0sD", TestEncryptionKey, true, false)]
    [TestCase("AQGJZim65rM+B45SIdkYPTWNRl9pZyKI6Aj1rIb/fQ27HcLTiu+U/vcEY2ttgpJPo20wUBP2BSC0fKOf4pI6yQ/TQB0nI4iJxvq/Pn5u5Keq23H2YqLDVwbakJ9I2/+Z0uo=", TestEncryptionKey, true, true)]
    public void DecryptWithAes_DifferentStrings_ReturnsValueDifferentFromInput(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var actual = input.DecryptWithAes(key, withDateTime, Int32.MaxValue, useSlowerButMoreSecureMethod);

        // Assert
        actual.Should().NotBeNullOrEmpty("because we expect an encrypted string");
        actual.Should().Be(TestValue, "because we expect the string to be decrypted");
    }

    [Test]
    [TestCase(TestValue, TestEncryptionKey, false, false)]
    [TestCase(TestValue, TestEncryptionKey, false, true)]
    [TestCase(TestValue, TestEncryptionKey, true, false)]
    [TestCase(TestValue, TestEncryptionKey, true, true)]
    public void EncryptWithAesWithSalt_DifferentStrings_ReturnsDifferentValueEachTime(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var actual1 = input.EncryptWithAesWithSalt(key, withDateTime, useSlowerButMoreSecureMethod);
        var actual2 = input.EncryptWithAesWithSalt(key, withDateTime, useSlowerButMoreSecureMethod);

        // Assert
        actual1.Should().NotBeNullOrEmpty("because we expect an encrypted string");
        actual2.Should().NotBeNullOrEmpty("because we expect an encrypted string");
        actual1.Should().NotBe(input, "because we expect the encrypted string to be different from the input");
        actual2.Should().NotBe(input, "because we expect the encrypted string to be different from the input");
        actual1.Should().NotBe(actual2, "because we expect the encrypted string to be different every time, because we're adding a salt");
    }

    [Test]
    [TestCase(TestValue, TestEncryptionKey, false, false)]
    [TestCase(TestValue, TestEncryptionKey, false, true)]
    [TestCase("HN4oS3nIfq4-quGA6BkoKRjHq7K0c9rfyXs2uAFXGt3plZMnpAQizCaw", TestOtherEncryptionKey, false, false)]
    [TestCase("AQEShUHLgDMiVyj2H/GvKScPvmH1rRSo1NTtvPfEN14G9Ua5OfZyz5BKA3W+j5IwVcFMKUJY7Nts01ZrLpzXZsKkyQDqfjMo76+BHcwxO9m0Di8q6hBVgIr5ojxESHejU24=", TestOtherEncryptionKey, false, true)]
    [TestCase("4ubkYLF-XfvVDv318hOY--zpyYdrg5IbybVKzRv7YiEfGlHc2wzStvN7OEoHQy+QaqMrn3FXcoC850M=", TestEncryptionKey, true, false)]
    [TestCase("AQGlbv4WS+FLRlrUBgo2HjPua3JB7RN6cQZSywPG8UTJJyKckiIn4mVEzfjt+wK1c50CXWxhV0azcxr4H/LE0jpM4PhF+4DaPjmD7WyCk3hgFoozZ1f0rWZnU+QGPNu0N4MtzPhPrg2OiBnQFdsiRd3x", TestEncryptionKey, true, true)]
    public void DecryptWithAesWithSalt_InvalidStrings_ThrowsException(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var comparison = () => { input.DecryptWithAesWithSalt(key, withDateTime, 1, useSlowerButMoreSecureMethod); };

        // Assert
        comparison.Should().Throw<Exception>("because we expect an exception to be thrown when trying to decrypt an invalid string");
    }

    [Test]
    [TestCase("HN4oS3nIfq4-quGA6BkoKRjHq7K0c9rfyXs2uAFXGt3plZMnpAQizCaw", TestEncryptionKey, false, false)]
    [TestCase("AQEShUHLgDMiVyj2H/GvKScPvmH1rRSo1NTtvPfEN14G9Ua5OfZyz5BKA3W+j5IwVcFMKUJY7Nts01ZrLpzXZsKkyQDqfjMo76+BHcwxO9m0Di8q6hBVgIr5ojxESHejU24=", TestEncryptionKey, false, true)]
    [TestCase("4ubkYLF-XfvVDv318hOY--zpyYdrg5IbybVKzRv7YiEfGlHc2wzStvN7OEoHQy+QaqMrn3FXcoC850M=", TestEncryptionKey, true, false)]
    [TestCase("AQGlbv4WS+FLRlrUBgo2HjPua3JB7RN6cQZSywPG8UTJJyKckiIn4mVEzfjt+wK1c50CXWxhV0azcxr4H/LE0jpM4PhF+4DaPjmD7WyCk3hgFoozZ1f0rWZnU+QGPNu0N4MtzPhPrg2OiBnQFdsiRd3x", TestEncryptionKey, true, true)]
    public void DecryptWithAesWithSalt_DifferentStrings_ReturnsValueDifferentFromInput(string input, string key, bool withDateTime, bool useSlowerButMoreSecureMethod)
    {
        // Act
        var actual = input.DecryptWithAesWithSalt(key, withDateTime, Int32.MaxValue, useSlowerButMoreSecureMethod);

        // Assert
        actual.Should().NotBeNullOrEmpty("because we expect an encrypted string");
        actual.Should().Be(TestValue, "because we expect the string to be decrypted");
    }

    [Test]
    [TestCase(TestValue)]
    public void ToSha512ForPasswords_DifferentStringsWithRandomSalt_ReturnsDifferentValueEachTime(string input)
    {
        // Act
        var actual1 = input.ToSha512ForPasswords();
        var actual2 = input.ToSha512ForPasswords();

        // Assert
        actual1.Should().NotBeNullOrEmpty("because we expect a hashed string");
        actual2.Should().NotBeNullOrEmpty("because we expect a hashed string");
        actual1.Should().NotBe(input, "because we expect the hashed string to be different from the input");
        actual2.Should().NotBe(input, "because we expect the hashed string to be different from the input");
        actual1.Should().NotBe(actual2, "because we expect the hashed string to be different every time, because we're adding a random salt");
    }

    [Test]
    [TestCase(TestValue)]
    public void ToSha512ForPasswords_DifferentStringsWithStaticSalt_ReturnsSameValueEachTime(string input)
    {
        // Prepare
        var saltBytes = Encoding.UTF8.GetBytes(TestEncryptionSalt);

        // Act
        var actual1 = input.ToSha512ForPasswords(saltBytes);
        var actual2 = input.ToSha512ForPasswords(saltBytes);

        // Assert
        actual1.Should().NotBeNullOrEmpty("because we expect a hashed string");
        actual1.Should().NotBe(input, "because we expect the hashed string to be different from the input");
        actual1.Should().Be(actual2, "because we expect the hashed string to be the same every time, because we're adding the same salt every time");
    }

    [Test]
    [TestCase("x", "pKvURIxJVi2CgRXROh/M6pJ/UrTVRZKX+LQ+QtqJI4vBNibkPcs43bCCSIkn7JBPtCBXRDmD6IWFF51QVRr+Yg==")]
    [TestCase(TestValue, "9fyn/zanltuXreNBQ88qDeLbsp39FMrdFWxZ64wDxzeRgETqM5Sa5/kSJinp21yLCFHR+jnXUXpdiDzXl3ihyg==")]
    public void ToSha512Simple_DifferentStrings_ReturnsHashedValue(string input, string expected)
    {
        // Act
        var actual = input.ToSha512Simple();

        // Assert
        actual.Should().NotBeNullOrEmpty("because we expect a hashed string");
        actual.Should().Be(expected, "because we expect a valid SHA512 hash");
    }

    [Test]
    [TestCase(TestValue, "tq9DY4mwVeGNDr2GFadaeO4CCkvnjuBOSbuTG/ibK0eE3XYvTcbqs34c4LxBl+PIoOwDga9G2WvMCnpTtwOuFhvLHfXttFY5+w==", true)]
    [TestCase(TestValue, "rY8LXXuz7hWAsCbIN6shTe+Rp4m0i3tmgfn8qebV7HuARafxf2lkgacq/u1H5zsyUbhxnn0VTY8KT2gl7MUE4s6Kf2l3jqnjOOyx", true)]
    [TestCase("x", "tq9DY4mwVeGNDr2GFadaeO4CCkvnjuBOSbuTG/ibK0eE3XYvTcbqs34c4LxBl+PIoOwDga9G2WvMCnpTtwOuFhvLHfXttFY5+w==", false)]
    [TestCase("x", "rY8LXXuz7hWAsCbIN6shTe+Rp4m0i3tmgfn8qebV7HuARafxf2lkgacq/u1H5zsyUbhxnn0VTY8KT2gl7MUE4s6Kf2l3jqnjOOyx", false)]
    public void VerifySha512_DifferentStringsAndHashes_ReturnsBoolean(string input, string hash, bool expected)
    {
        // Act
        var actual = input.VerifySha512(hash);

        // Assert
        actual.Should().Be(expected, "because we expect a valid SHA512 hash");
    }

    [Test]
    [TestCaseSource(nameof(GetTestCasesForToDictionary))]
    public void ToDictionary_DifferentStrings_ReturnsDictionary(string input, bool addSortInKey, bool useFirstEntryIfExist, Dictionary<string, string> expected)
    {
        // Prepare
        const string rowSplitter = "&";
        const string columnSplitter = "=";

        // Act
        var actual = input.ToDictionary(rowSplitter, columnSplitter, addSortInKey, useFirstEntryIfExist);

        // Assert
        actual.Should().BeEquivalentTo(expected, "because we expect a specific dictionary");
    }

    [Test]
    [TestCase("", "")]
    [TestCase("test", "Test")]
    [TestCase("Test", "Test")]
    [TestCase("this is a test", "This is a test")]
    public void CapitalizeFirst_DifferentStrings_ReturnsCapitalizedString(string input, string expected)
    {
        // Act
        var actual = input.CapitalizeFirst();

        // Assert
        actual.Should().Be(expected, "because we expect the first letter to be capitalized");
    }

    [Test]
    [TestCase("", "")]
    [TestCase(TestValue, TestValue)]
    [TestCase("Test+test2", "Test_test2")]
    [TestCase("this&test2", "this_test2")]
    [TestCase(@"this+&!@#$%^&*()/\<>;:'test2", @"this__!@#$%^_*()/\<>;:'test2")]
    public void StripIllegalFilenameCharacters_DifferentStrings_ReturnsValidPathNames(string input, string expected)
    {
        // Act
        var actual = input.StripIllegalFilenameCharacters();

        // Assert
        actual.Should().Be(expected, "because we expect a valid file name");
    }

    [Test]
    [TestCase("", "")]
    [TestCase(TestValue, TestValue)]
    [TestCase("Test+test2", "Test_test2")]
    [TestCase("this&test2", "this_test2")]
    [TestCase(@"this+&!@#$%^&*()/\<>;:'test2", @"this__!@#$%^_*()/\<>;:'test2")]
    public void StripIllegalPathCharacters_DifferentStrings_ReturnsValidPathNames(string input, string expected)
    {
        // Act
        var actual = input.StripIllegalPathCharacters();

        // Assert
        actual.Should().Be(expected, "because we expect a valid path name");
    }

    /// <summary>
    /// Function to get test cases for the ToDictionary method.
    /// This method needs a dictionary, which cannot be created in the TestCase attribute.
    /// </summary>
    /// <returns>The values for the parameters of the test methods for ToDictionary.</returns>
    private static IEnumerable<object[]> GetTestCasesForToDictionary()
    {
        return new object[][]
        {
            // Simple strings with one variable.
            new object[] {
                "simple=test",
                false,
                false,
                new Dictionary<string, string>
                {
                    { "simple", "test" }
                }
            },
            new object[] {
                "simple=test",
                true,
                false,
                new Dictionary<string, string>
                {
                    { "0000_simple", "test" }
                }
            },
            new object[] {
                "simple=test&simple=test2",
                false,
                false,
                new Dictionary<string, string>
                {
                    { "simple", "test2" }
                }
            },
            new object[] {
                "simple=test&simple=test2",
                false,
                true,
                new Dictionary<string, string>
                {
                    { "simple", "test" }
                }
            },
            new object[] {
                "simple=test&simple2=test2",
                false,
                true,
                new Dictionary<string, string>
                {
                    { "simple", "test" },
                    { "simple2", "test2" }
                }
            },
            new object[] {
                "simple=test&simple2=test2",
                true,
                true,
                new Dictionary<string, string>
                {
                    { "0000_simple", "test" },
                    { "0001_simple2", "test2" }
                }
            }
        };
    }
}