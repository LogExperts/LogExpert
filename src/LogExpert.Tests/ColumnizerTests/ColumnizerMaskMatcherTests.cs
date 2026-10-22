using LogExpert.Core.Classes.Columnizer;
using LogExpert.Core.Config;

using NUnit.Framework;

namespace LogExpert.Tests.ColumnizerTests;

[TestFixture]
public class ColumnizerMaskMatcherTests
{
    private static ColumnizerMaskEntry Entry (string mask, MaskType type) => new() { Mask = mask, Type = type, ColumnizerName = "X" };

    // Glob tests

    [Test]
    public void GlobStarLog_MatchesFooLog ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("*.log", MaskType.Glob), "foo.log"), Is.True);
    }

    [Test]
    public void GlobStarLog_DoesNotMatchFooTxt ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("*.log", MaskType.Glob), "foo.txt"), Is.False);
    }

    [Test]
    public void GlobQuestion_MatchesSingleChar ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("?.log", MaskType.Glob), "a.log"), Is.True);
    }

    [Test]
    public void GlobQuestion_DoesNotMatchTwoChars ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("?.log", MaskType.Glob), "aa.log"), Is.False);
    }

    [Test]
    public void Glob_IsCaseInsensitive ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("*.LOG", MaskType.Glob), "foo.log"), Is.True);
    }

    [Test]
    public void Glob_EscapesRegexMetacharacters ()
    {
        // "+" must be treated literally in glob mode
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("foo+bar.log", MaskType.Glob), "foo+bar.log"), Is.True);
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("foo+bar.log", MaskType.Glob), "fooXbar.log"), Is.False);
    }

    [Test]
    public void Glob_DotIsLiteral ()
    {
        // "my.log" glob: dot must match literal dot, not any char
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("my.log", MaskType.Glob), "my.log"), Is.True);
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("my.log", MaskType.Glob), "myXlog"), Is.False);
    }

    // Regex tests

    [Test]
    public void Regex_MatchesAnchoredPattern ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry(@".+\.log$", MaskType.Regex), "foo.log"), Is.True);
    }

    [Test]
    public void Regex_DoesNotMatchUnrelated ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry(@".+\.log$", MaskType.Regex), "foo.txt"), Is.False);
    }

    [Test]
    public void Regex_MalformedPatternReturnsFalse ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("[", MaskType.Regex), "foo.log"), Is.False);
    }

    // Null / empty input

    [Test]
    public void NullMask_ReturnsFalse ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry(null!, MaskType.Glob), "foo.log"), Is.False);
    }

    [Test]
    public void EmptyMask_ReturnsFalse ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry(string.Empty, MaskType.Glob), "foo.log"), Is.False);
    }

    [Test]
    public void NullFileName_ReturnsFalse ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(Entry("*.log", MaskType.Glob), null!), Is.False);
    }

    [Test]
    public void NullEntry_ReturnsFalse ()
    {
        Assert.That(ColumnizerMaskMatcher.Matches(null!, "foo.log"), Is.False);
    }
}
