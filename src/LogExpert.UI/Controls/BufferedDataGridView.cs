using System.Drawing.Drawing2D;
using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.Core.Entities;
using LogExpert.Core.EventArguments;
using LogExpert.UI.Controls;

using NLog;

namespace LogExpert.Dialogs;

[SupportedOSPlatform("windows")]
internal partial class BufferedDataGridView : DataGridView
{
    #region Fields

    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Brush _brush;

    private readonly Color _bubbleColor = Color.FromArgb(160, 250, 250, 0); //yellow
    private readonly Font _font = new("Arial", 10);

    private readonly SortedList<int, BookmarkOverlay> _overlayList = [];

    private readonly Pen _pen;
    private readonly Brush _textBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 90)); //dark blue

    private BookmarkOverlay _draggedOverlay;
    private Point _dragStartPoint;
    private bool _isDrag;
    private Size _oldOverlayOffset;

    #endregion

    #region cTor

    public BufferedDataGridView ()
    {
        _pen = new Pen(_bubbleColor, (float)3.0);
        _brush = new SolidBrush(_bubbleColor);

        InitializeComponent();
        DoubleBuffered = true;
        VirtualMode = true;
    }

    #endregion

    #region Events

    public event EventHandler<OverlayEventArgs> OverlayDoubleClicked;

    #endregion

    #region Properties

    /*
  public Graphics Buffer
  {
    get { return this.myBuffer.Graphics; }
  }
   */

    public ContextMenuStrip EditModeMenuStrip { get; set; }

    public bool PaintWithOverlays { get; set; }

    #endregion

    #region Public methods

    public void AddOverlay (BookmarkOverlay overlay)
    {
        lock (_overlayList)
        {
            _overlayList.Add(overlay.Position.Y, overlay);
        }
    }

    #endregion

    #region Overrides

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
        }
        catch (Exception ex)
        {
            _logger.Error(ex);
        }
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
        if (_isDrag)
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

    #endregion

    #region Private Methods

    private BookmarkOverlay GetOverlayForPosition (Point pos)
    {
        lock (_overlayList)
        {
            foreach (var overlay in _overlayList.Values)
            {
                if (overlay.BubbleRect.Contains(pos))
                {
                    return overlay;
                }
            }
        }

        return null;
    }

    private void PaintOverlays (PaintEventArgs e)
    {
        var currentContext = BufferedGraphicsManager.Current;

        using var myBuffer = currentContext.Allocate(CreateGraphics(), ClientRectangle);
        lock (_overlayList)
        {
            _overlayList.Clear();
        }

        myBuffer.Graphics.SetClip(ClientRectangle, CombineMode.Union);
        e.Graphics.SetClip(ClientRectangle, CombineMode.Union);

        PaintEventArgs args = new(myBuffer.Graphics, e.ClipRectangle);

        base.OnPaint(args);

        StringFormat format = new()
        {
            LineAlignment = StringAlignment.Center,
            Alignment = StringAlignment.Near
        };

        myBuffer.Graphics.SetClip(DisplayRectangle, CombineMode.Intersect);

        // Remove Columnheader from Clippingarea
        Rectangle rectTableHeader = new(DisplayRectangle.X, DisplayRectangle.Y, DisplayRectangle.Width, ColumnHeadersHeight);
        myBuffer.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);

        //e.Graphics.SetClip(rect, CombineMode.Union);

        lock (_overlayList)
        {
            foreach (var overlay in _overlayList.Values)
            {
                var textSize = myBuffer.Graphics.MeasureString(overlay.Bookmark.Text, _font, 300);
                Rectangle rectBubble = new(overlay.Position, new Size((int)textSize.Width, (int)textSize.Height));
                rectBubble.Offset(60, -(rectBubble.Height + 40));
                rectBubble.Inflate(3, 3);
                rectBubble.Location += overlay.Bookmark.OverlayOffset;
                overlay.BubbleRect = rectBubble;
                myBuffer.Graphics.SetClip(rectBubble, CombineMode.Union); // Bubble to clip
                myBuffer.Graphics.SetClip(rectTableHeader, CombineMode.Exclude);
                e.Graphics.SetClip(rectBubble, CombineMode.Union);
                RectangleF textRect = new(rectBubble.X, rectBubble.Y, rectBubble.Width, rectBubble.Height);
                myBuffer.Graphics.FillRectangle(_brush, rectBubble);
                //myBuffer.Graphics.DrawLine(_pen, overlay.Position, new Point(rect.X, rect.Y + rect.Height / 2));
                myBuffer.Graphics.DrawLine(_pen, overlay.Position, new Point(rectBubble.X, rectBubble.Y + rectBubble.Height));
                myBuffer.Graphics.DrawString(overlay.Bookmark.Text, _font, _textBrush, textRect, format);

                if (_logger.IsDebugEnabled)
                {
                    _logger.Debug(CultureInfo.InvariantCulture, $"ClipRgn: {myBuffer.Graphics.ClipBounds.Left},{myBuffer.Graphics.ClipBounds.Top},{myBuffer.Graphics.ClipBounds.Width},{myBuffer.Graphics.ClipBounds.Height}");
                }
            }
        }

        myBuffer.Render(e.Graphics);
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
                else
                {
                    _logger.Warn(CultureInfo.InvariantCulture, "Edit control was null, to be checked");
                }
            }
        }
    }

    protected virtual void OnOverlayDoubleClicked (OverlayEventArgs e)
    {
        OverlayDoubleClicked?.Invoke(this, e);
    }

    #endregion
}
