using System.ComponentModel;
using System.Globalization;

using LogExpert.Core.Classes.Marker;
using LogExpert.Core.EventArguments;

namespace LogExpert.UI.Controls.LogWindow;

/// <summary>Renders prepared marker buckets. It has no reader, matching logic or scan lifecycle.</summary>
internal sealed class MarkerBar : Control
{
    private static readonly MarkerCategory[] _categoryOrder = [MarkerCategory.Highlights, MarkerCategory.Bookmarks, MarkerCategory.Search, MarkerCategory.Filter];
    private static readonly IReadOnlyDictionary<MarkerCategory, IReadOnlyList<MarkerBucket>> _emptyLanes = new Dictionary<MarkerCategory, IReadOnlyList<MarkerBucket>>();
    private readonly ToolTip _toolTip = new();
    private readonly ContextMenuStrip _menu = new();
    private IReadOnlyDictionary<MarkerCategory, IReadOnlyList<MarkerBucket>> _lanes = _emptyLanes;
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

    internal void SetBuckets (IReadOnlyDictionary<MarkerCategory, IReadOnlyList<MarkerBucket>> lanes, int height, bool discovering)
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
        _lanes = _emptyLanes;
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
        for (var laneIndex = 0; laneIndex < _categoryOrder.Length; laneIndex++)
        {
            var left = laneIndex * ClientSize.Width / _categoryOrder.Length;
            var right = (laneIndex + 1) * ClientSize.Width / _categoryOrder.Length;
            if (laneIndex > 0)
            {
                e.Graphics.DrawLine(separator, left, TopInset, left, TopInset + BucketHeight);
            }

            if (_bucketHeight != BucketHeight || right <= left)
            {
                continue;
            }

            foreach (var bucket in GetBuckets(_categoryOrder[laneIndex]))
            {
                brush.Color = Color.FromArgb(bucket.ColorArgb);
                var inset = right - left > 1 ? 1 : 0;
                e.Graphics.FillRectangle(brush, left + inset, TopInset + bucket.Pixel, right - left - inset, 1);
            }
        }

        if (_discovering)
        {
            brush.Color = ForeColor;
            var scale = DeviceDpi / 96f;
            var diameter = 2 * scale;
            for (var dot = -1; dot <= 1; dot++)
            {
                e.Graphics.FillEllipse(brush, Width / 2f + dot * 4 * scale - diameter / 2,
                    Math.Max(diameter, TopInset / 2f), diameter, diameter);
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
            ? string.Format(CultureInfo.CurrentCulture, Resources.MarkerBar_ToolTip, CategoryName(hit.Value.Category),
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

    private (MarkerCategory Category, MarkerBucket Bucket)? HitTest (Point point)
    {
        if (ClientSize.Width <= 0 || point.X < 0 || point.X >= ClientSize.Width || _bucketHeight != BucketHeight)
        {
            return null;
        }

        var laneIndex = Math.Min(_categoryOrder.Length - 1, ((point.X + 1) * _categoryOrder.Length - 1) / ClientSize.Width);
        var category = _categoryOrder[laneIndex];
        var pixel = point.Y - TopInset;
        foreach (var bucket in GetBuckets(category))
        {
            if (bucket.Pixel == pixel)
            {
                return (category, bucket);
            }
        }

        return null;
    }

    private IReadOnlyList<MarkerBucket> GetBuckets (MarkerCategory category)
    {
        return _lanes.GetValueOrDefault(category) ?? [];
    }

    private static string CategoryName (MarkerCategory category)
    {
        return category switch
        {
            MarkerCategory.Highlights => Resources.MarkerBar_Highlights,
            MarkerCategory.Bookmarks => Resources.MarkerBar_Bookmarks,
            MarkerCategory.Search => Resources.MarkerBar_SearchHits,
            MarkerCategory.Filter => Resources.MarkerBar_FilterHits,
            _ => throw new ArgumentOutOfRangeException(nameof(category), category, null)
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