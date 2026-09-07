using System.ComponentModel;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

using LogExpert.Core.Entities;
using LogExpert.Core.Config;
using LogExpert.Core.EventArguments;
using LogExpert.UI.Entities;

using NLog;

namespace LogExpert.UI.Controls;

[SupportedOSPlatform("windows")]
internal partial class BufferedDataGridView : DataGridView
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    private static Color BubbleColor =>
        Application.IsDarkModeEnabled
            ? Color.FromArgb(160, 80, 80, 0) // muted yellow on dark
            : Color.FromArgb(160, 250, 250, 0); // bright yellow on light

    private static Color TextColor =>
        Application.IsDarkModeEnabled
            ? Color.FromArgb(200, 180, 200, 255) // light blue on dark
            : Color.FromArgb(200, 0, 0, 90); // dark blue on light

    private readonly Font _font = new("Segoe UI", 9.75f);
    private Pen? _pen;
    private Brush? _brush;
    private Brush? _textBrush;
    private Color _currentBubbleColor;
    private Color _currentTextColor;

    private readonly StringFormat _format = new()
    {
        LineAlignment = StringAlignment.Center,
        Alignment = StringAlignment.Near
    };

    private readonly Lock _overlayLock = new();
    private readonly List<BookmarkOverlay> _overlayStaging = [];
    private BookmarkOverlay[] _overlaySnapshot = [];

    private BookmarkOverlay? _draggedOverlay;
    private Point _dragStartPoint;
    private bool _isDrag;
    private Size _oldOverlayOffset;

    #endregion

    #region cTor

    public BufferedDataGridView ()
    {
        InitializeComponent();
        DoubleBuffered = true;
        VirtualMode = true;
    }

    #endregion

    #region Events

    public event EventHandler<OverlayEventArgs> OverlayDoubleClicked;

    #endregion

    #region Properties

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public ContextMenuStrip EditModeMenuStrip { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool PaintWithOverlays { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public SelectionHighlightSettings SelectionHighlight { get; set; } = new();

    #endregion

    #region Public methods

    public void AddOverlay (BookmarkOverlay overlay)
    {
        lock (_overlayLock)
        {
            _overlayStaging.Add(overlay);
        }
    }

    /// <summary>
    /// Atomically captures all staged overlays and clears the staging list. Call this once per paint cycle before
    /// drawing.
    /// </summary>
    private BookmarkOverlay[] SwapOverlaySnapshot ()
    {
        lock (_overlayLock)
        {
            _overlaySnapshot = [.. _overlayStaging];
            _overlayStaging.Clear();

            return _overlaySnapshot;
        }
    }

    /// <summary>
    /// Ensures GDI+ drawing resources match the current color mode.
    /// Called at the start of each paint cycle.
    /// </summary>
    private void EnsureDrawingResources ()
    {
        var bubbleColor = BubbleColor;
        var textColor = TextColor;

        if (bubbleColor == _currentBubbleColor
            && textColor == _currentTextColor
            && _pen is not null)
        {
            return;
        }

        _pen?.Dispose();
        _brush?.Dispose();
        _textBrush?.Dispose();

        _currentBubbleColor = bubbleColor;
        _currentTextColor = textColor;

        _pen = new Pen(_currentBubbleColor, 3.0f);
        _brush = new SolidBrush(_currentBubbleColor);
        _textBrush = new SolidBrush(_currentTextColor);
    }

    #endregion

    #region Overrides

    protected override void OnSelectionChanged (EventArgs e)
    {
        base.OnSelectionChanged(e);
        if (SelectionHighlight?.Outline == true)
        {
            // A changed cell can also remove an edge on a still-selected neighbor.
            Invalidate();
        }
    }

    protected override void OnGotFocus (EventArgs e)
    {
        base.OnGotFocus(e);
        Invalidate();
    }

    protected override void OnScroll (ScrollEventArgs e)
    {
        base.OnScroll(e);
        if (SelectionHighlight?.Outline == true)
        {
            Invalidate();
        }
    }

    protected override void OnLostFocus (EventArgs e)
    {
        base.OnLostFocus(e);
        Invalidate();
    }

    protected override void Dispose (bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _brush?.Dispose();
            _pen?.Dispose();
            _textBrush?.Dispose();
            _font?.Dispose();
            _format?.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override void OnPaint (PaintEventArgs e)
    {
        try
        {
            if (PaintWithOverlays)
            {
                PaintOverlays(e);
            }
            else
            {
                base.OnPaint(e);
            }

            SelectionPainter.PaintOutline(this, e, SelectionHighlight);
        }
        catch (Exception ex)
        {
            _logger.Error($"Overlay painting failed, falling back to base paint. {ex}");

            try
            {
                base.OnPaint(e);
            }
            catch (InvalidOperationException innerEx)
            {
                _logger.Error($"Base paint also failed. {innerEx}");
            }
        }
    }

    private void PaintOverlays (PaintEventArgs e)
    {
        EnsureDrawingResources();

        // Let the base DataGridView paint into its own double buffer first.
        base.OnPaint(e);

        // Atomically capture and clear staged overlays. No lock held after this.
        var overlays = SwapOverlaySnapshot();

        if (overlays.Length == 0)
        {
            return;
        }

        // Save the original clip and set up overlay clipping area.
        var originalClip = e.Graphics.Clip;

        e.Graphics.SetClip(DisplayRectangle, CombineMode.Replace);

        // Exclude column headers from overlay drawing area.
        Rectangle rectTableHeader = new(
            DisplayRectangle.X,
            DisplayRectangle.Y,
            DisplayRectangle.Width,
            ColumnHeadersHeight);

        e.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);

        foreach (var overlay in overlays)
        {
            var textSize = e.Graphics.MeasureString(overlay.Bookmark.Text, _font, 300);

            Rectangle rectBubble = new(
                overlay.Position,
                new Size((int)textSize.Width,
                (int)textSize.Height));

            rectBubble.Offset(60, -(rectBubble.Height + 40));
            rectBubble.Inflate(3, 3);
            rectBubble.Location += overlay.Bookmark.OverlayOffset;
            overlay.BubbleRect = rectBubble;

            // Temporarily extend clip to include the bubble area.
            e.Graphics.SetClip(rectBubble, CombineMode.Union);
            e.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);

            RectangleF textRect = new(
                rectBubble.X,
                rectBubble.Y,
                rectBubble.Width,
                rectBubble.Height);

            e.Graphics.FillRectangle(_brush, rectBubble);
            e.Graphics.DrawLine(
                _pen,
                overlay.Position,
                new Point(rectBubble.X, rectBubble.Y + rectBubble.Height));
            e.Graphics.DrawString(overlay.Bookmark.Text, _font, _textBrush, textRect, _format);

            if (_logger.IsDebugEnabled)
            {
                _logger.Debug($"### PaintOverlays: {e.Graphics.ClipBounds.Left}, {e.Graphics.ClipBounds.Top}, {e.Graphics.ClipBounds.Width}, {e.Graphics.ClipBounds.Height}");
            }
        }

        // Restore original clip region.
        e.Graphics.Clip = originalClip;
    }

    protected override void OnEditingControlShowing (DataGridViewEditingControlShowingEventArgs e)
    {
        base.OnEditingControlShowing(e);
        e.Control.KeyDown -= OnControlKeyDown;
        e.Control.KeyDown += OnControlKeyDown;
        var editControl = (DataGridViewTextBoxEditingControl)e.Control;
        e.Control.PreviewKeyDown -= OnControlPreviewKeyDown;
        e.Control.PreviewKeyDown += OnControlPreviewKeyDown;

        editControl.ContextMenuStrip = EditModeMenuStrip;
    }

    protected override void OnMouseDown (MouseEventArgs e)
    {
        var overlay = GetOverlayForPosition(e.Location);
        if (overlay != null)
        {
            if (e.Button == MouseButtons.Right)
            {
                if (_isDrag)
                {
                    _isDrag = false;
                    overlay.Bookmark.OverlayOffset = _oldOverlayOffset;
                    Refresh();
                }
            }
            else
            {
                _dragStartPoint = e.Location;
                _isDrag = true;
                _draggedOverlay = overlay;
                _oldOverlayOffset = overlay.Bookmark.OverlayOffset;
            }
        }
        else
        {
            _isDrag = false;
            base.OnMouseDown(e);
        }
    }

    protected override void OnMouseUp (MouseEventArgs e)
    {
        if (_isDrag)
        {
            _isDrag = false;
            Refresh();
        }
        else
        {
            base.OnMouseUp(e);
        }
    }

    protected override void OnMouseMove (MouseEventArgs e)
    {
        if (_isDrag && _draggedOverlay is not null)
        {
            Cursor = Cursors.Hand;
            Size offset = new(e.X - _dragStartPoint.X, e.Y - _dragStartPoint.Y);
            _draggedOverlay.Bookmark.OverlayOffset = _oldOverlayOffset + offset;
            Refresh();
        }
        else
        {
            var overlay = GetOverlayForPosition(e.Location);
            Cursor = overlay != null ? Cursors.Hand : Cursors.Default;
            base.OnMouseMove(e);
        }
    }

    protected override void OnMouseDoubleClick (MouseEventArgs e)
    {
        var overlay = GetOverlayForPosition(e.Location);
        if (overlay != null)
        {
            if (e.Button == MouseButtons.Left)
            {
                OnOverlayDoubleClicked(new OverlayEventArgs(overlay));
            }
        }
        else
        {
            base.OnMouseDoubleClick(e);
        }
    }

    protected override void OnMouseLeave (EventArgs e)
    {
        if (!_isDrag)
        {
            Cursor = Cursors.Default;
        }

        base.OnMouseLeave(e);
    }

    #endregion

    #region Private Methods

    private BookmarkOverlay GetOverlayForPosition (Point pos)
    {
        var overlays = _overlaySnapshot;

        foreach (var overlay in overlays)
        {
            if (overlay.BubbleRect.Contains(pos))
            {
                return overlay;
            }
        }

        return null;
    }

    #endregion

    #region Events handler

    private void OnControlPreviewKeyDown (object sender, PreviewKeyDownEventArgs e)
    {
        if ((e.KeyCode == Keys.C || e.KeyCode == Keys.Insert) && e.Control)
        {
            if (EditingControl != null)
            {
                e.IsInputKey = true;
            }
        }
    }

    private void OnControlKeyDown (object sender, KeyEventArgs e)
    {
        if (e.KeyCode is Keys.Up or Keys.Down)
        {
            if (EditingControl != null)
            {
                if (EditingControl is LogCellEditingControl editControl)
                {
                    _ = editControl.EditingControlDataGridView.EndEdit();
                    var line = editControl.EditingControlDataGridView.CurrentCellAddress.Y;
                    if (e.KeyCode == Keys.Up)
                    {
                        if (line > 0)
                        {
                            line--;
                        }
                    }

                    if (e.KeyCode == Keys.Down)
                    {
                        if (line < editControl.EditingControlDataGridView.RowCount - 1)
                        {
                            line++;
                        }
                    }

                    var col = editControl.EditingControlDataGridView.CurrentCellAddress.X;
                    var scrollIndex = editControl.EditingControlDataGridView.HorizontalScrollingOffset;
                    var selStart = editControl.SelectionStart;
                    editControl.EditingControlDataGridView.CurrentCell = editControl.EditingControlDataGridView.Rows[line].Cells[col];
                    _ = editControl.EditingControlDataGridView.BeginEdit(false);
                    editControl.SelectionStart = selStart;
                    editControl.ScrollToCaret();
                    editControl.EditingControlDataGridView.HorizontalScrollingOffset = scrollIndex;
                    e.Handled = true;
                }
                //else
                //{
                //    _logger.Warn($"Edit control was null, to be checked");
                //}
            }
        }
    }

    protected virtual void OnOverlayDoubleClicked (OverlayEventArgs e)
    {
        OverlayDoubleClicked?.Invoke(this, e);
    }

    #endregion
}
