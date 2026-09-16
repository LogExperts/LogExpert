using System.ComponentModel;
using System.Globalization;

using LogExpert.Core.Classes.Marker;
using LogExpert.Core.EventArguments;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>Renders prepared marker buckets. It has no reader, matching logic or scan lifecycle.</summary>
internal sealed class MarkerBar : Control
{
    private readonly ToolTip _toolTip = new();
    private readonly ContextMenuStrip _menu = new();
    private IReadOnlyList<MarkerBucket>[] _lanes = [[], [], [], []];
    private int _bucketHeight;
    private bool _discovering;
    private string _tooltipText = string.Empty;

    public MarkerBar ()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        AccessibleName = Resources.MarkerBar_Title;
        TabStop = false;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        _menu.Items.Add(Resources.MarkerBar_ClearSearch, null, (_, _) => ClearSearchRequested?.Invoke(this, EventArgs.Empty));
        ContextMenuStrip = _menu;
    }

    public event EventHandler<SelectLineEventArgs>? LineSelected;
    public event EventHandler? ClearSearchRequested;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int TopInset { get; set; }
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal int BottomInset { get; set; }
    internal int BucketHeight => Math.Max(0, ClientSize.Height - TopInset - BottomInset);

    internal void SetBuckets (IReadOnlyList<MarkerBucket>[] lanes, int height, bool discovering)
    {
        _lanes = lanes;
        _bucketHeight = height;
        SetDiscovering(discovering);
        Invalidate();
    }

    internal void SetDiscovering (bool discovering)
    {
        if (_discovering != discovering)
        {
            _discovering = discovering;
            AccessibleDescription = discovering ? Resources.MarkerBar_Discovering : Resources.MarkerBar_Title;
            Invalidate();
        }
    }

    internal void ClearBuckets ()
    {
        _lanes = [[], [], [], []];
        _tooltipText = string.Empty;
        _toolTip.SetToolTip(this, null);
        Invalidate();
    }

    protected override void OnPaint (PaintEventArgs e)
    {
        base.OnPaint(e);
        if (ClientSize.Width <= 0)
        {
            return;
        }

        using var brush = new SolidBrush(ForeColor);
        using var separator = new Pen(SystemColors.ControlDark);
        for (var lane = 0; lane < 4; lane++)
        {
            var left = lane * ClientSize.Width / 4;
            var right = (lane + 1) * ClientSize.Width / 4;
            if (lane > 0)
            {
                e.Graphics.DrawLine(separator, left, TopInset, left, TopInset + BucketHeight);
            }

            if (_bucketHeight != BucketHeight || right <= left)
            {
                continue;
            }

            foreach (var bucket in _lanes[lane])
            {
                brush.Color = Color.FromArgb(bucket.ColorArgb);
                var inset = right - left > 1 ? 1 : 0;
                e.Graphics.FillRectangle(brush, left + inset, TopInset + bucket.Pixel, right - left - inset, 1);
            }
        }

        if (_discovering)
        {
            brush.Color = ForeColor;
            for (var dot = -1; dot <= 1; dot++)
            {
                e.Graphics.FillEllipse(brush, Width / 2 + dot * 4 - 1, Math.Max(2, TopInset / 2), 2, 2);
            }
        }
    }

    protected override void OnMouseUp (MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left && HitTest(e.Location) is { } hit)
        {
            LineSelected?.Invoke(this, new SelectLineEventArgs(hit.Bucket.TargetLine));
        }
    }

    protected override void OnMouseMove (MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hit = HitTest(e.Location);
        var text = hit.HasValue
            ? string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ToolTip, CategoryName(hit.Value.Lane),
                hit.Value.Bucket.FirstLine + 1, hit.Value.Bucket.LastLine + 1, hit.Value.Bucket.Count)
            : Resources.MarkerBar_Title;
        if (_discovering)
        {
            text += Environment.NewLine + Resources.MarkerBar_Discovering;
        }

        if (!string.Equals(text, _tooltipText, StringComparison.Ordinal))
        {
            _tooltipText = text;
            _toolTip.SetToolTip(this, text);
        }
    }

    private (int Lane, MarkerBucket Bucket)? HitTest (Point point)
    {
        if (ClientSize.Width <= 0 || point.X < 0 || point.X >= ClientSize.Width || _bucketHeight != BucketHeight)
        {
            return null;
        }

        var lane = Math.Min(3, ((point.X + 1) * 4 - 1) / ClientSize.Width);
        var pixel = point.Y - TopInset;
        foreach (var bucket in _lanes[lane])
        {
            if (bucket.Pixel == pixel)
            {
                return (lane, bucket);
            }
        }

        return null;
    }

    private static string CategoryName (int lane)
    {
        return lane switch
        {
            0 => Resources.MarkerBar_Highlights,
            1 => Resources.MarkerBar_Bookmarks,
            2 => Resources.MarkerBar_SearchHits,
            _ => Resources.MarkerBar_FilterHits
        };
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            _toolTip.Dispose();
            _menu.Dispose();
        }

        base.Dispose(disposing);
    }
}