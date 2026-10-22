using ColumnizerLib;

using LogExpert.Core.Classes.Attributes;
using LogExpert.Core.Classes.JsonConverters;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

public class MockColumnizer : ILogLineMemoryColumnizer
{
    [JsonColumnizerProperty]
    public int IntProperty { get; set; }

    [JsonColumnizerProperty]
    public string StringProperty { get; set; }

    public string GetName () => "MockColumnizer";

    public string GetCustomName () => GetName();

    public string GetDescription () => "Test columnizer";

    public int GetColumnCount () => 1;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Unit Test")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unit Test")]
    public string GetColumnName (int column) => "Col";

    public bool IsTimeshiftImplemented () => false;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Unit Test")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unit Test")]
    public void SetColumnNames (string[] names) { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Unit Test")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unit Test")]
    public void SetParameters (string param) { }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Unit Test")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Unit Test")]
    public void SetConfig (object config) { }

    public string[] GetColumnNames () => throw new NotImplementedException();

    public void SetTimeOffset (int msecOffset) => throw new NotImplementedException();

    public int GetTimeOffset () => throw new NotImplementedException();

    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine) => throw new NotImplementedException();

    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine) => throw new NotImplementedException();

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue) => throw new NotImplementedException();

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue) => throw new NotImplementedException();
}

public class MockColumnizerWithCustomName : ILogLineMemoryColumnizer
{
    [JsonColumnizerProperty]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1721:Property names should not match get methods", Justification = "Unit Test")]
    public string CustomName { get; set; } = "DefaultName";

    [JsonColumnizerProperty]
    public int Value { get; set; }

    public string GetName () => CustomName;

    public string GetCustomName () => GetName();

    public string GetDescription () => "Test columnizer with custom name";

    public int GetColumnCount () => 1;

    public string[] GetColumnNames () => ["Column1"];

    public bool IsTimeshiftImplemented () => false;

    public void SetTimeOffset (int msecOffset) => throw new NotImplementedException();

    public int GetTimeOffset () => throw new NotImplementedException();

    public IColumnizedLogLineMemory SplitLine (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        throw new NotImplementedException();
    }

    public DateTime GetTimestamp (ILogLineMemoryColumnizerCallback callback, ILogLineMemory logLine)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, string oldValue)
    {
        throw new NotImplementedException();
    }

    public void PushValue (ILogLineMemoryColumnizerCallback callback, int column, string value, ReadOnlyMemory<char> oldValue)
    {
        throw new NotImplementedException();
    }

}

public sealed class NotAColumnizer
{
    public static int ConstructorCalls { get; set; }

    public NotAColumnizer ()
    {
        ConstructorCalls++;
    }
}

[TestFixture]
public class ColumnizerJsonConverterTests
{
    [Test]
    public void Deserialize_TypeNameIsNotAColumnizer_ThrowsWithoutCreatingInstance ()
    {
        NotAColumnizer.ConstructorCalls = 0;
        var json = JsonConvert.SerializeObject(new
        {
            TypeName = typeof(NotAColumnizer).AssemblyQualifiedName,
            State = new { }
        });

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new ColumnizerJsonConverter() }
        };

        _ = Assert.Throws<JsonSerializationException>(() => JsonConvert.DeserializeObject<ILogLineMemoryColumnizer>(json, settings));
        Assert.That(NotAColumnizer.ConstructorCalls, Is.Zero);
    }

    [Test]
    public void SerializeDeserialize_MockColumnizer_RoundTripPreservesStateAndType ()
    {
        var original = new MockColumnizer
        {
            IntProperty = 42,
            StringProperty = "TestValue"
        };

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new ColumnizerJsonConverter() }
        };

        var json = JsonConvert.SerializeObject(original, settings);
        var deserialized = JsonConvert.DeserializeObject<ILogLineMemoryColumnizer>(json, settings);

        Assert.That(deserialized, Is.Not.Null);
        Assert.That(original.GetName(), Is.EqualTo(deserialized.GetName()));
        Assert.That(42, Is.EqualTo(((MockColumnizer)deserialized).IntProperty));
        Assert.That("TestValue", Is.EqualTo(((MockColumnizer)deserialized).StringProperty));
    }

    [Test]
    public void SerializeDeserialize_CustomNamedColumnizer_PreservesCustomName ()
    {
        // Arrange: Create a columnizer with a custom name
        var original = new MockColumnizerWithCustomName
        {
            CustomName = "MyCustomRegex",
            Value = 123
        };

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new ColumnizerJsonConverter() }
        };

        // Act: Serialize and deserialize
        var json = JsonConvert.SerializeObject(original, settings);
        var deserialized = JsonConvert.DeserializeObject<ILogLineMemoryColumnizer>(json, settings) as MockColumnizerWithCustomName;

        // Assert: Verify the custom name and state are preserved
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.GetName(), Is.EqualTo("MyCustomRegex"));
        Assert.That(deserialized.CustomName, Is.EqualTo("MyCustomRegex"));
        Assert.That(deserialized.Value, Is.EqualTo(123));
    }

    [Test]
    public void SerializeDeserialize_UsesTypeNameNotDisplayName ()
    {
        // Arrange: Create a columnizer with a custom name that differs from type name
        var original = new MockColumnizerWithCustomName
        {
            CustomName = "CompletelyDifferentName",
            Value = 456
        };

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new ColumnizerJsonConverter() }
        };

        // Act: Serialize
        var json = JsonConvert.SerializeObject(original, settings);

        // Assert: JSON should contain TypeName (assembly qualified) not just the display name
        Assert.That(json, Does.Contain("TypeName"));
        Assert.That(json, Does.Contain("MockColumnizerWithCustomName"));

        // Act: Deserialize
        var deserialized = JsonConvert.DeserializeObject<ILogLineMemoryColumnizer>(json, settings) as MockColumnizerWithCustomName;

        // Assert: Should successfully deserialize even though GetName() returns different value
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.CustomName, Is.EqualTo("CompletelyDifferentName"));
        Assert.That(deserialized.Value, Is.EqualTo(456));
    }

    [Test]
    public void Deserialize_BackwardCompatibility_CanReadOldFormat ()
    {
        // Arrange: Old format JSON (using "Type" instead of "TypeName")
        var oldFormatJson = @"{
            ""Type"": ""MockColumnizer"",
            ""DisplayName"": ""MockColumnizer"",
            ""State"": {
                ""IntProperty"": 99,
                ""StringProperty"": ""OldValue""
            }
        }";

        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.None,
            Converters = { new ColumnizerJsonConverter() }
        };

        // Act: Deserialize old format
        var deserialized = JsonConvert.DeserializeObject<ILogLineMemoryColumnizer>(oldFormatJson, settings) as MockColumnizer;

        // Assert: Should successfully deserialize using fallback logic
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.IntProperty, Is.EqualTo(99));
        Assert.That(deserialized.StringProperty, Is.EqualTo("OldValue"));
    }
}