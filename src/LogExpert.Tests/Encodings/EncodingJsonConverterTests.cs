using System.Text;

using LogExpert.Core.Classes.JsonConverters;
using LogExpert.Core.Helpers;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.Encodings;

/// <summary>
/// The converter used for every <see cref="Encoding"/> in the settings JSON. It runs during
/// <c>ConfigManager</c> initialisation — long before any dialog exists — so it must be able to resolve a
/// legacy Windows code page on its own.
/// </summary>
[TestFixture]
public class EncodingJsonConverterTests
{
    [Test]
    [TestCase("windows-1250", 1250)]
    [TestCase("windows-1252", 1252)]
    [TestCase("gb2312", 936)]
    public void ReadJson_LegacyCodePageName_ResolvesInsteadOfFallingBack (string encodingName, int expectedCodePage)
    {
        var encoding = Deserialize($"\"{encodingName}\"");

        Assert.Multiple(() =>
        {
            Assert.That(encoding, Is.Not.Null);
            Assert.That(encoding.CodePage, Is.EqualTo(expectedCodePage));
        });
    }

    [Test]
    public void ReadJson_Null_ReturnsNull ()
    {
        Assert.That(Deserialize("null"), Is.Null);
    }

    [Test]
    [TestCase("\"\"")]
    [TestCase("\"not-a-real-encoding-xxxxx\"")]
    public void ReadJson_UnusableName_ReturnsDefaultEncoding (string json)
    {
        Assert.That(Deserialize(json), Is.EqualTo(Encoding.Default));
    }

    [Test]
    public void WriteJson_RoundTripsALegacyCodePage ()
    {
        // Resolve through the registry, not Encoding.GetEncoding — otherwise this test would depend on
        // some earlier test having registered the provider.
        var written = JsonConvert.SerializeObject(EncodingRegistry.GetEncoding(1252), new EncodingJsonConverter());

        Assert.That(Deserialize(written)?.CodePage, Is.EqualTo(1252));
    }

    private static Encoding? Deserialize (string json)
    {
        return JsonConvert.DeserializeObject<Encoding>(json, new EncodingJsonConverter());
    }
}
