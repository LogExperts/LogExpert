using System.Globalization;
using System.Reflection;

using LogExpert.Core.Classes.Marker;
using LogExpert.UI.Controls.LogWindow;

using NUnit.Framework;

namespace LogExpert.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class MarkerBarTests
{
    [Test]
    public void Paint_RendersEachLaneBucketWithItsColor ()
    {
        using var bar = CreateBar(40, 10);
        bar.SetBuckets(
        [
            [new MarkerBucket(2, 10, 19, 1, 12, Color.Red.ToArgb())],
            [new MarkerBucket(2, 20, 29, 1, 22, Color.Green.ToArgb())],
            [new MarkerBucket(2, 30, 39, 1, 32, Color.Blue.ToArgb())],
            [new MarkerBucket(2, 40, 49, 1, 42, Color.Purple.ToArgb())]
        ], 10, discovering: false);

        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);

        Assert.Multiple(() =>
        {
            Assert.That(image.GetPixel(5, 2).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
            Assert.That(image.GetPixel(15, 2).ToArgb(), Is.EqualTo(Color.Green.ToArgb()));
            Assert.That(image.GetPixel(25, 2).ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
            Assert.That(image.GetPixel(35, 2).ToArgb(), Is.EqualTo(Color.Purple.ToArgb()));
        });
    }

    [Test]
    public void MouseUp_OnBucketRaisesTargetLine_AndEmptyClickDoesNothing ()
    {
        using var bar = CreateBar(40, 10);
        bar.SetBuckets([[new MarkerBucket(2, 10, 19, 3, 14, Color.Red.ToArgb())], [], [], []], 10, false);
        var selected = new List<int>();
        bar.LineSelected += (_, args) => selected.Add(args.Line);

        RaiseMouse(bar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 2, 0));
        RaiseMouse(bar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 5, 3, 0));

        Assert.That(selected, Is.EqualTo([14]));
    }

    [Test]
    public void MouseMove_TooltipIncludesCategoryRangeAndCount ()
    {
        using var bar = CreateBar(40, 10);
        bar.SetBuckets([[new MarkerBucket(2, 10, 19, 3, 14, Color.Red.ToArgb())], [], [], []], 10, false);

        RaiseMouse(bar, "OnMouseMove", new MouseEventArgs(MouseButtons.None, 0, 5, 2, 0));

        var toolTip = (ToolTip)typeof(MarkerBar).GetField("_toolTip", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(bar)!;
        var expected = string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ToolTip, Resources.MarkerBar_Highlights, 11, 20, 3);
        Assert.That(toolTip.GetToolTip(bar), Is.EqualTo(expected));
    }

    [Test]
    public void SmallOddWidth_AndResizeWithStaleHeight_DoNotSelectBuckets ()
    {
        using var bar = CreateBar(5, 10);
        bar.SetBuckets([[new MarkerBucket(2, 10, 19, 1, 14, Color.Red.ToArgb())], [], [], []], 10, false);
        var selected = new List<int>();
        bar.LineSelected += (_, args) => selected.Add(args.Line);

        RaiseMouse(bar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 0, 2, 0));
        Assert.That(selected, Is.EqualTo([14]));

        bar.Height = 12;
        RaiseMouse(bar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, 1, 2, 0));

        Assert.That(selected, Is.EqualTo([14]));
    }

    [Test]
    public void SmallOddWidth_RendersEachLaneWithinItsVisibleColumn ()
    {
        using var bar = CreateBar(5, 10);
        bar.SetBuckets(
        [
            [new MarkerBucket(2, 10, 19, 1, 10, Color.Red.ToArgb())],
            [new MarkerBucket(2, 20, 29, 1, 20, Color.Green.ToArgb())],
            [new MarkerBucket(2, 30, 39, 1, 30, Color.Blue.ToArgb())],
            [new MarkerBucket(2, 40, 49, 1, 40, Color.Purple.ToArgb())]
        ], 10, false);

        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);

        Assert.Multiple(() =>
        {
            Assert.That(image.GetPixel(0, 2).ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
            Assert.That(image.GetPixel(1, 2).ToArgb(), Is.EqualTo(Color.Green.ToArgb()));
            Assert.That(image.GetPixel(2, 2).ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
            Assert.That(image.GetPixel(3, 2).ToArgb(), Is.EqualTo(SystemColors.ControlDark.ToArgb()));
            Assert.That(image.GetPixel(4, 2).ToArgb(), Is.EqualTo(Color.Purple.ToArgb()));
        });
    }

    [TestCase("light", 255, 255, 255)]
    [TestCase("dark", 32, 32, 32)]
    public void Paint_UsesExplicitBackgroundForLightAndDarkModes (string _, int red, int green, int blue)
    {
        using var bar = CreateBar(40, 10);
        bar.BackColor = Color.FromArgb(red, green, blue);
        bar.ForeColor = Color.White;
        bar.SetBuckets([[], [], [], []], 10, false);

        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);

        Assert.That(image.GetPixel(0, 0).ToArgb(), Is.EqualTo(bar.BackColor.ToArgb()));
    }

    [TestCase(28, 400, 4, 8)]
    [TestCase(42, 600, 6, 12)]
    [TestCase(56, 800, 8, 16)]
    public void ScaledGeometry_PaintsAndSelectsTheLastBucket (int width, int height, int topInset, int bottomInset)
    {
        using var bar = CreateBar(width, height);
        bar.TopInset = topInset;
        bar.BottomInset = bottomInset;
        var bucketHeight = height - topInset - bottomInset;
        var pixel = bucketHeight - 1;
        bar.SetBuckets([[], [], [], [new MarkerBucket(pixel, 90, 99, 1, 95, Color.Orange.ToArgb())]], bucketHeight, false);
        var selected = new List<int>();
        bar.LineSelected += (_, args) => selected.Add(args.Line);

        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);
        RaiseMouse(bar, "OnMouseUp", new MouseEventArgs(MouseButtons.Left, 1, width - 1, topInset + pixel, 0));

        Assert.Multiple(() =>
        {
            Assert.That(image.GetPixel(width - 1, topInset + pixel).ToArgb(), Is.EqualTo(Color.Orange.ToArgb()));
            Assert.That(selected, Is.EqualTo([95]));
        });
    }

    [Test]
    public void DrawToBitmap_ProducesMarkerBarArtifact ()
    {
        using var bar = CreateBar(80, 16);
        bar.SetBuckets([[new MarkerBucket(4, 20, 29, 2, 24, Color.Orange.ToArgb())], [], [], []], 16, false);
        using var image = new Bitmap(bar.Width, bar.Height);
        bar.DrawToBitmap(image, bar.ClientRectangle);
        var path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "marker-bar.png");
        image.Save(path, System.Drawing.Imaging.ImageFormat.Png);
        TestContext.AddTestAttachment(path);
        Assert.That(File.Exists(path), Is.True);
    }

    private static MarkerBar CreateBar (int width, int height)
    {
        var bar = new MarkerBar { Size = new Size(width, height) };
        _ = bar.Handle;
        return bar;
    }

    private static void RaiseMouse (MarkerBar bar, string methodName, MouseEventArgs args)
    {
        var method = typeof(Control).GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!;
        _ = method.Invoke(bar, [args]);
    }
}