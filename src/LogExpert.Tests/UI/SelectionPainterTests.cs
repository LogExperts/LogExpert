using LogExpert.Core.Config;
using LogExpert.UI.Entities;
using LogExpert.UI.Controls;
using LogExpert.UI.Interface;
using LogExpert.Core.Classes.Highlight;
using LogExpert.Core.Entities;
using ColumnizerLib;

using Moq;

using NUnit.Framework;

namespace LogExpert.Tests.UI;

[TestFixture]
[Apartment(ApartmentState.STA)]
[System.Runtime.Versioning.SupportedOSPlatform("windows")]
public class SelectionPainterTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void LogContentPainting_OutlinePreservesLineAndWordBackgrounds (bool outline)
    {
        var settings = new SelectionHighlightSettings { Outline = outline, CustomColor = Color.Magenta };
        var context = new Mock<ILogPaintContextUI>();
        context.SetupGet(c => c.SelectionHighlight).Returns(settings);
        context.Setup(c => c.GetLogLineMemory(It.IsAny<int>())).Returns(new LogLine("WORD plain", 0));
        context.Setup(c => c.FindHighlightEntry(It.IsAny<ITextValueMemory>(), true))
            .Returns(new HighlightEntry { BackgroundColor = Color.LightSalmon, ForegroundColor = Color.Black });
        context.Setup(c => c.FindHighlightMatches(It.IsAny<ITextValueMemory>())).Returns(() => new List<HighlightMatchEntry>
        {
            new() { StartPos = 0, Length = 4, HighlightEntry = new() { IsWordMatch = true, BackgroundColor = Color.Yellow, ForegroundColor = Color.Red } }
        });
        context.SetupGet(c => c.NormalFont).Returns(() => new Font("Consolas", 10));
        context.SetupGet(c => c.BoldFont).Returns(() => new Font("Consolas", 10, FontStyle.Bold));
        using var form = new Form { ClientSize = new Size(320, 180) };
        using var grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill, AllowUserToAddRows = false, RowHeadersVisible = false,
            ColumnHeadersVisible = false, CellBorderStyle = DataGridViewCellBorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect, SelectionHighlight = settings
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn { Width = 280 });
        grid.RowCount = 2;
        grid.CellValueNeeded += (_, e) => e.Value = new Column { FullValue = "WORD plain".AsMemory() };
        grid.CellPainting += (_, e) => PaintHelper.CellPainting(context.Object, grid.Focused, e.RowIndex, e.ColumnIndex, e);
        form.Controls.Add(grid);
        form.Show();
        grid.Rows[0].Selected = true;
        using var bitmap = new Bitmap(grid.Width, grid.Height);
        grid.DrawToBitmap(bitmap, grid.ClientRectangle);
        var bounds = grid.GetCellDisplayRectangle(0, 0, false);
        Assert.That(bitmap.GetPixel(bounds.Left + 4, bounds.Top + 3).ToArgb(), Is.EqualTo((outline ? Color.Yellow : Color.Magenta).ToArgb()));
        Assert.That(bitmap.GetPixel(bounds.Right - 10, bounds.Top + 3).ToArgb(), Is.EqualTo((outline ? Color.LightSalmon : Color.Magenta).ToArgb()));
    }

    [Test]
    public void PaintedBlock_HasNoInternalEdge_AndErasesOldEdgesAfterSelectionChanges ()
    {
        using var form = new Form { ClientSize = new Size(320, 220) };
        using var grid = new BufferedDataGridView
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            RowHeadersVisible = false,
            ColumnHeadersVisible = false,
            CellBorderStyle = DataGridViewCellBorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            SelectionHighlight = new() { Outline = true, CustomColor = Color.Magenta }
        };
        grid.Columns.Add("a", "A");
        grid.Columns.Add("b", "B");
        grid.Rows.Add(4);
        // Supply highlighted content beneath the selection overlay.
        grid.DefaultCellStyle.BackColor = Color.Khaki;
        grid.DefaultCellStyle.SelectionBackColor = Color.Khaki;
        form.Controls.Add(grid);
        form.Show();
        grid.ClearSelection();
        grid.Rows[0].Selected = true;
        grid.Rows[1].Selected = true;
        using var bitmap = new Bitmap(grid.Width, grid.Height);
        grid.DrawToBitmap(bitmap, grid.ClientRectangle);
        var first = grid.GetCellDisplayRectangle(0, 0, false);
        var second = grid.GetCellDisplayRectangle(0, 1, false);

        Assert.That(bitmap.GetPixel(first.Left + 20, first.Top).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
        Assert.That(bitmap.GetPixel(first.Left + 20, first.Bottom - 1).ToArgb(), Is.EqualTo(Color.Khaki.ToArgb()));
        Assert.That(bitmap.GetPixel(second.Left + 20, second.Bottom - 1).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));

        grid.Rows[1].Selected = false;
        grid.DrawToBitmap(bitmap, grid.ClientRectangle);
        Assert.That(bitmap.GetPixel(first.Left + 20, first.Bottom - 1).ToArgb(), Is.EqualTo(Color.Magenta.ToArgb()));
        Assert.That(bitmap.GetPixel(second.Left + 20, second.Bottom - 1).ToArgb(), Is.EqualTo(Color.Khaki.ToArgb()));
    }

    [Test]
    public void Outline_PreservesBlackAndColoredText ()
    {
        var style = SelectionPainter.GetStyle(new() { Outline = true, CustomColor = Color.Yellow }, true, true, Color.Blue, false);
        Assert.That(style.FillBackground, Is.False);
        Assert.That(style.Foreground(Color.Black), Is.EqualTo(Color.Black));
        Assert.That(style.Foreground(Color.Red), Is.EqualTo(Color.Red));
    }

    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(false, true)]
    public void CustomFill_UsesReadableTextEvenWithoutFocus (bool focused, bool darkMode)
    {
        var style = SelectionPainter.GetStyle(new() { CustomColor = Color.Yellow }, true, focused, Color.Blue, darkMode);
        Assert.That(style.Background, Is.EqualTo(Color.Yellow));
        Assert.That(style.Foreground(Color.White), Is.EqualTo(Color.Black));
    }

    [Test]
    public void DefaultFill_PreservesLegacyForegroundRules ()
    {
        var style = SelectionPainter.GetStyle(new(), true, true, Color.Blue, false);
        Assert.That(style.Foreground(Color.Black), Is.EqualTo(Color.White));
        Assert.That(style.Foreground(Color.Red), Is.EqualTo(Color.Red));
    }

    [Test]
    public void DisjointRows_EachHaveTopAndBottomBoundaries ()
    {
        using var grid = CreateGrid();
        grid.Rows[0].Selected = true;
        grid.Rows[2].Selected = true;
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 0), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Bottom | SelectionEdges.Left));
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 2, 0), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Bottom | SelectionEdges.Left));
    }

    [Test]
    public void CellSelection_UsesVisibleDisplayOrder ()
    {
        using var grid = CreateGrid();
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        grid.Columns.Add("hidden", "Hidden");
        grid.Columns[2].Visible = false;
        grid.Columns[1].DisplayIndex = 0;
        grid[0, 0].Selected = true;
        grid[1, 0].Selected = true;
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 1), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Bottom | SelectionEdges.Left));
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 0), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Bottom | SelectionEdges.Right));

        grid[1, 0].Selected = false;
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 0), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Bottom | SelectionEdges.Left | SelectionEdges.Right));
    }

    [Test]
    public void AdjacentRows_HaveOnlyAnExternalOutline ()
    {
        using var grid = CreateGrid();
        grid.Rows[0].Selected = true;
        grid.Rows[1].Selected = true;

        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 0), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Left));
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 0, 1), Is.EqualTo(SelectionEdges.Top | SelectionEdges.Right));
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 1, 0), Is.EqualTo(SelectionEdges.Bottom | SelectionEdges.Left));
        Assert.That(SelectionPainter.GetOutlineEdges(grid, 2, 0), Is.EqualTo(SelectionEdges.None));
    }

    private static DataGridView CreateGrid ()
    {
        var grid = new DataGridView { AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };
        grid.Columns.Add("a", "A");
        grid.Columns.Add("b", "B");
        grid.Rows.Add(4);
        grid.ClearSelection();
        return grid;
    }
}