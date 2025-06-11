using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using NUnit.Framework;

namespace GeeksCoreLibrary.Tests.Core.Models;

[SuppressMessage("ReSharper", "RedundantAssignment")]
[SuppressMessage("ReSharper", "JoinDeclarationAndInitializer")]
public class GclRequestContextTests
{
    [SetUp]
    public void Setup()
    {
        // Reset context before each test.
        var properties = GetTestableProperties();
        foreach (var propertyInfo in properties)
        {
            // Reset each property to its default value.
            propertyInfo.SetValue(null, propertyInfo.PropertyType.GetDefaultValue());
        }
    }

    [Test]
    public void EncryptionKey_CanBeSetAndRetrieved()
    {
        // Dynamically get all properties of GclRequestContext that are testable, so that we can test them all.
        var properties = GetTestableProperties();
        foreach (var property in properties)
        {
            // Arrange
            var (testValue, _, _) = GetTestValues(property.PropertyType);
            var defaultValue = property.PropertyType.GetDefaultValue();
            object? actual;
            object? afterReset;

            // Act
            property.SetValue(null, testValue);
            actual = property.GetValue(null);
            property.SetValue(null, defaultValue);
            afterReset = property.GetValue(null);

            // Assert
            actual.Should().Be(testValue, $"because the property '{property.Name}' should return what was set");
            afterReset.Should().Be(defaultValue, $"Property {property.Name} should be reset to default");
        }
    }

    [Test]
    public void EncryptionKey_IsIsolatedAcrossAsyncFlows()
    {
        // Dynamically get all properties of GclRequestContext that are testable, so that we can test them all.
        var properties = GetTestableProperties();
        foreach (var property in properties)
        {
            // Arrange
            var (mainThreadValue, firstTaskValue, secondTaskValue) = GetTestValues(property.PropertyType);

            object? valueInParentBeforeFirstTaskChange = null;
            object? valueInParentAfterFirstTaskChange = null;
            object? valueInParentBeforeSecondTaskChange = null;
            object? valueInParentAfterSecondTaskChange = null;
            object? valueInFirstTaskBeforeChange = null;
            object? valueInFirstTaskAfterChange = null;
            object? valueInSecondTaskBeforeChange = null;
            object? valueInSecondTaskAfterChange = null;

            // Act
            // We start without a value in the parent context.
            valueInParentBeforeFirstTaskChange = property.GetValue(null);

            // Run the first task to set a value in the async context, this task should start with null and end with the value of secondKey.
            var firstTask = Task.Run(() =>
            {
                // In a new async context, should be null.
                valueInFirstTaskBeforeChange = property.GetValue(null);

                // Now set to another value in this context.
                property.SetValue(null, firstTaskValue);
                valueInFirstTaskAfterChange = property.GetValue(null);
            });

            firstTask.Wait();

            // The task should not have changed the parent context, so this should still be null.
            valueInParentAfterFirstTaskChange = property.GetValue(null);

            // Now we set the main thread value, which should stay the same after the second task has finished.
            property.SetValue(null, mainThreadValue);
            valueInParentBeforeSecondTaskChange = property.GetValue(null);

            // Run the second task to set a value in the async context, this task should start with the value of the main thread and end with the value of secondTaskValue.
            var secondTask = Task.Run(() =>
            {
                // In a new async context, should be null or default.
                valueInSecondTaskBeforeChange = property.GetValue(null);

                // Now set to another value in this context.
                property.SetValue(null, secondTaskValue);
                valueInSecondTaskAfterChange = property.GetValue(null);
            });

            secondTask.Wait();

            // The task should not have changed the parent context, so this should still be the main thread value.
            valueInParentAfterSecondTaskChange = property.GetValue(null);

            // Assert
            valueInParentBeforeFirstTaskChange.Should().BeNull("because the initial value should always be null");
            valueInParentAfterFirstTaskChange.Should().BeNull("because the async task should not change the parent context");

            valueInParentBeforeSecondTaskChange.Should().Be(mainThreadValue, "because the main thread should now have its own value set");
            valueInParentAfterSecondTaskChange.Should().Be(mainThreadValue, "because the async task should not change the parent context");

            valueInFirstTaskBeforeChange.Should().BeNull("because the task should copy the context when it starts");
            valueInFirstTaskAfterChange.Should().Be(firstTaskValue, "because the task should have its own context that can be changed independently");

            valueInSecondTaskBeforeChange.Should().Be(mainThreadValue, "because the task should copy the context when it starts");
            valueInSecondTaskAfterChange.Should().Be(secondTaskValue, "because the task should have its own context that can be changed independently");
        }
    }

    /// <summary>
    /// Get the properties of <see cref="GclRequestContext"/> that are testable.
    /// </summary>
    /// <returns>A list of properties for testing.</returns>
    private static PropertyInfo[] GetTestableProperties()
    {
        return typeof(GclRequestContext)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p is {CanRead: true, CanWrite: true})
            .ToArray();
    }

    /// <summary>
    /// Get test values for the given type.
    /// </summary>
    /// <param name="type">The <see cref="Type"/> to get test values for.</param>
    /// <returns>A tuple with three test values.</returns>
    /// <exception cref="NotSupportedException">When a <see cref="Type"/> is used that we don't support yet.</exception>
    private static (object FirstValue, object SecondValue, object ThirdValue) GetTestValues(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
        {
            return ("FirstValue", "SecondValue", "ThirdValue");
        }

        if (underlyingType == typeof(int) || underlyingType == typeof(uint) || underlyingType == typeof(long) || underlyingType == typeof(ulong) || underlyingType == typeof(decimal))
        {
            return (42, 1337, 314159);
        }

        if (underlyingType == typeof(Guid))
        {
            return (Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        }

        // Add more types as needed
        throw new NotSupportedException($"Please add support for {type.Name}");
    }
}