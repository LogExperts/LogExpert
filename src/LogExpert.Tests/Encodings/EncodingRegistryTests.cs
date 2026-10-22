using System.Text;

using LogExpert.Core.Helpers;

using NUnit.Framework;

namespace LogExpert.Tests.Encodings;

/// <summary>
/// The legacy Windows code pages used to become resolvable only as a side effect of constructing
/// the Preferences dialog. Anything that resolved an encoding name earlier — Preferences, a .lxp,
/// the settings JSON — silently fell back to <see cref="Encoding.Default"/>. These tests pin the
/// guarantee that resolving goes through <see cref="EncodingRegistry"/> instead.
/// </summary>
[TestFixture]
public class EncodingRegistryTests
{
    [Test]
    [TestCase(1250)]
    [TestCase(1252)]
    [TestCase(936)]
    public void GetEncoding_LegacyCodePage_Resolves (int codePage)
    {
        var encoding = EncodingRegistry.GetEncoding(codePage);

        Assert.That(encoding.CodePage, Is.EqualTo(codePage));
    }

    [Test]
    [TestCase("windows-1250")]
    [TestCase("windows-1252")]
    [TestCase("utf-8")]
    [TestCase("iso-8859-1")]
    [TestCase("gb2312")]
    public void TryGetEncoding_SupportedName_ReturnsTrueAndEncoding (string name)
    {
        var resolved = EncodingRegistry.TryGetEncoding(name, out var encoding);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.True);
            Assert.That(encoding.WebName, Is.EqualTo(name));
        });
    }

    [Test]
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-a-real-encoding-xxxxx")]
    public void TryGetEncoding_UnusableName_ReturnsFalse (string? name)
    {
        var resolved = EncodingRegistry.TryGetEncoding(name, out var encoding);

        Assert.Multiple(() =>
        {
            Assert.That(resolved, Is.False);
            Assert.That(encoding, Is.Null);
        });
    }

}
