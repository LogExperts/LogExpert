using System.Drawing;
using System.Linq;

using LogExpert.Core.Config;

using Newtonsoft.Json;

using NUnit.Framework;

namespace LogExpert.Tests.ConfigManagerTests;

[TestFixture]
public class ControlCharSettingsTests
{
    [Test]
    public void Default_Substitute_IsFalse ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.Substitute, Is.False);
    }

    [Test]
    public void Default_Style_IsControlPictures ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.Style, Is.EqualTo(ControlCharStyle.ControlPictures));
    }

    [Test]
    public void Default_ForeColor_IsGray ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.ForeColor, Is.EqualTo(Color.Gray));
    }

    [Test]
    public void Default_BackColor_IsEmpty ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.BackColor, Is.EqualTo(Color.Empty));
    }

    [Test]
    public void Default_BoldAndItalic_AreFalse ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.Bold, Is.False);
        Assert.That(settings.Italic, Is.False);
    }

    [Test]
    public void Default_CopyDisplayedForm_IsFalse ()
    {
        var settings = new ControlCharSettings();
        Assert.That(settings.CopyDisplayedForm, Is.False);
    }

    [Test]
    public void Default_EnabledCodepoints_IsNonWhitespacePreset ()
    {
        var settings = new ControlCharSettings();

        var expected = Enumerable.Range(0x00, 0x20)
            .Where(c => c != 0x09 && c != 0x0A && c != 0x0D)
            .Append(0x7F)
            .ToArray();

        Assert.That(settings.EnabledCodepoints, Is.EquivalentTo(expected));
        Assert.That(settings.EnabledCodepoints.Count, Is.EqualTo(30));
    }

    [Test]
    public void RoundTrip_DefaultInstance_PreservesAllProperties ()
    {
        var original = new ControlCharSettings();

        string json = JsonConvert.SerializeObject(original);
        var roundTripped = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Substitute, Is.EqualTo(original.Substitute));
        Assert.That(roundTripped.Style, Is.EqualTo(original.Style));
        Assert.That(roundTripped.ForeColor.ToArgb(), Is.EqualTo(original.ForeColor.ToArgb()));
        Assert.That(roundTripped.BackColor.ToArgb(), Is.EqualTo(original.BackColor.ToArgb()));
        Assert.That(roundTripped.Bold, Is.EqualTo(original.Bold));
        Assert.That(roundTripped.Italic, Is.EqualTo(original.Italic));
        Assert.That(roundTripped.CopyDisplayedForm, Is.EqualTo(original.CopyDisplayedForm));
        Assert.That(roundTripped.EnabledCodepoints, Is.EquivalentTo(original.EnabledCodepoints));
    }

    [Test]
    public void RoundTrip_CustomisedInstance_PreservesAllProperties ()
    {
        var original = new ControlCharSettings
        {
            Substitute = true,
            Style = ControlCharStyle.Caret,
            ForeColor = Color.FromArgb(255, 200, 100, 50),
            BackColor = Color.FromArgb(255, 10, 20, 30),
            Bold = true,
            Italic = true,
            CopyDisplayedForm = true,
            EnabledCodepoints = [0x01, 0x02, 0x7F],
        };

        string json = JsonConvert.SerializeObject(original);
        var roundTripped = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(roundTripped, Is.Not.Null);
        Assert.That(roundTripped!.Substitute, Is.True);
        Assert.That(roundTripped.Style, Is.EqualTo(ControlCharStyle.Caret));
        Assert.That(roundTripped.ForeColor.ToArgb(), Is.EqualTo(original.ForeColor.ToArgb()));
        Assert.That(roundTripped.BackColor.ToArgb(), Is.EqualTo(original.BackColor.ToArgb()));
        Assert.That(roundTripped.Bold, Is.True);
        Assert.That(roundTripped.Italic, Is.True);
        Assert.That(roundTripped.CopyDisplayedForm, Is.True);
        Assert.That(roundTripped.EnabledCodepoints, Is.EquivalentTo(new[] { 0x01, 0x02, 0x7F }));
    }

    [Test]
    public void Deserialize_EnabledCodepointsNull_FallsBackToPreset ()
    {
        const string json = "{ \"EnabledCodepoints\": null }";

        var settings = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.EnabledCodepoints, Is.Not.Null);
        Assert.That(settings.EnabledCodepoints.Count, Is.EqualTo(30));
    }

    [Test]
    public void Deserialize_EnabledCodepointsEmptyArray_StaysEmpty ()
    {
        const string json = "{ \"EnabledCodepoints\": [] }";

        var settings = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.EnabledCodepoints, Is.Empty);
    }

    [Test]
    public void Deserialize_StyleOutOfRange_FallsBackToControlPictures ()
    {
        const string json = "{ \"Style\": 99 }";

        var settings = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.Style, Is.EqualTo(ControlCharStyle.ControlPictures));
    }

    [TestCase(0, ControlCharStyle.ControlPictures)]
    [TestCase(1, ControlCharStyle.Caret)]
    [TestCase(2, ControlCharStyle.CEscape)]
    [TestCase(3, ControlCharStyle.Abbreviation)]
    [TestCase(4, ControlCharStyle.Iso2047)]
    public void RoundTrip_StyleNumericValue_MapsToNamedStyle (int numeric, ControlCharStyle expected)
    {
        string json = $"{{ \"Style\": {numeric} }}";

        var settings = JsonConvert.DeserializeObject<ControlCharSettings>(json);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.Style, Is.EqualTo(expected));

        string reserialised = JsonConvert.SerializeObject(settings);
        var reloaded = JsonConvert.DeserializeObject<ControlCharSettings>(reserialised);

        Assert.That(reloaded!.Style, Is.EqualTo(expected));
    }
}
