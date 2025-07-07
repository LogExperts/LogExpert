using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.Core.Classes;
using LogExpert.Core.EventArguments;
using LogExpert.UI.Extensions;

using NLog;

namespace LogExpert.UI.Controls.LogWindow;

[SupportedOSPlatform("windows")]
internal partial class TimeSpreadingControl : UserControl
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    #region Fields

    private Bitmap _bitmap = new(1, 1);
    private int _displayHeight = 1;
    private readonly int _edgeOffset = (int)NativeMethods.GetSystemMetricsForDpi(NativeMethods.SM_CYVSCROLL);
    private int _lastMouseY;
    private readonly object _monitor = new();
    private int _rectHeight = 1;

    private TimeSpreadCalculator _timeSpreadCalc;
    private readonly ToolTip _toolTip;

    #endregion

    #region cTor

    public TimeSpreadingControl ()
    {
        InitializeComponent();
        _toolTip = new ToolTip();
        Font = new Font("Courier New", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
        _toolTip.InitialDelay = 0;
        _toolTip.ReshowDelay = 0;
        _toolTip.ShowAlways = true;
        DoubleBuffered = false;
    }

    #endregion

    #region Delegates

    #endregion

    #region Events

    public EventHandler<SelectLineEventArgs> LineSelected;

    #endregion

    #region Properties

    public bool ReverseAlpha { get; set; }

    internal TimeSpreadCalculator TimeSpreadCalc
    {
        get => _timeSpreadCalc;
        set
        {
            //timeSpreadCalc.CalcDone -= timeSpreadCalc_CalcDone;
            _timeSpreadCalc = value;
            _timeSpreadCalc.CalcDone += OnTimeSpreadCalcCalcDone;
            _timeSpreadCalc.StartCalc += OnTimeSpreadCalcStartCalc;
        }
    }

    #endregion

    #region Overrides

    protected override void OnPaint (PaintEventArgs e)
    {
        base.OnPaint(e);
        lock (_monitor)
        {
            if (DesignMode)
            {
                using var bgBrush = new SolidBrush(Color.FromKnownColor(KnownColor.LightSkyBlue));
                var rect = ClientRectangle;
                rect.Inflate(0, -_edgeOffset);
                e.Graphics.FillRectangle(bgBrush, rect);
            }
            else
            {
                e.Graphics.DrawImage(_bitmap, 0, _edgeOffset);
            }
        }
    }

    #endregion

    #region Private Methods

    private SpreadEntry GetEntryForMouse (MouseEventArgs e)
    {
        var list = TimeSpreadCalc.DiffList;
        var y = e.Y - _edgeOffset;
        if (y < 0)
        {
            y = 0;
        }
        else if (y >= ClientRectangle.Height - (_edgeOffset * 3))
        {
            y = list.Count - 1;
        }
        else
        {
            y /= _rectHeight;
        }

        lock (_monitor)
        {
            return y >= list.Count || y < 0
                ? null
                : list[y];
        }
    }

    private void DragContrast (MouseEventArgs e)
    {
        if (_lastMouseY == 0)
        {
            _lastMouseY = _lastMouseY = e.Y;
            return;
        }

        _timeSpreadCalc.Contrast += (_lastMouseY - e.Y) * 5;
        _lastMouseY = e.Y;
    }

    private void OnLineSelected (SelectLineEventArgs e)
    {
        LineSelected?.Invoke(this, e);
    }

    #endregion

    #region Events handler

    private void OnTimeSpreadCalcCalcDone (object sender, EventArgs e)
    {
        _logger.Debug(CultureInfo.InvariantCulture, "timeSpreadCalc_CalcDone()");

        lock (_monitor)
        {
            Invalidate();
            var rect = ClientRectangle;
            rect.Size = new Size(rect.Width, rect.Height - (_edgeOffset * 3));

            if (rect.Height < 1)
            {
                return;
            }

            _bitmap = new Bitmap(rect.Width, rect.Height);
            using var gfx = Graphics.FromImage(_bitmap);
            using var bgBrush = new SolidBrush(BackColor);
            gfx.FillRectangle(bgBrush, rect);

            var list = TimeSpreadCalc.DiffList;
            int step;

            if (list.Count >= _displayHeight)
            {
                step = (int)Math.Round(list.Count / (double)_displayHeight);
                _rectHeight = 1;
            }
            else
            {
                step = 1;
                _rectHeight = (int)Math.Round(_displayHeight / (double)list.Count);
            }

            Rectangle fillRect = new(0, 0, rect.Width, _rectHeight);

            lock (list)
            {
                for (var i = 0; i < list.Count; i += step)
                {
                    var entry = list[i];
                    var color = ReverseAlpha ? entry.Value : 255 - entry.Value;

                    if (color > 255)
                    {
                        color = 255;
                    }

                    if (color < 0)
                    {
                        color = 0;
                    }

                    using var brush = new SolidBrush(Color.FromArgb(color, ForeColor));
                    gfx.FillRectangle(brush, fillRect);
                    fillRect.Offset(0, _rectHeight);
                }
            }
        }

        BeginInvoke(new MethodInvoker(Refresh));
    }

    private void OnTimeSpreadCalcStartCalc (object sender, EventArgs e)
    {
        lock (_monitor)
        {
            Invalidate();
            var rect = ClientRectangle;
            rect.Size = new Size(rect.Width, rect.Height - (_edgeOffset * 3));

            if (rect.Height < 1)
            {
                return;
            }

            //this.bmp = new Bitmap(rect.Width, rect.Height);
            var gfx = Graphics.FromImage(_bitmap);

            using var bgBrush = new SolidBrush(BackColor);
            using var fgBrush = new SolidBrush(ForeColor);
            //gfx.FillRectangle(bgBrush, rect);

            StringFormat format = new(StringFormatFlags.DirectionVertical | StringFormatFlags.NoWrap)
            {
                LineAlignment = StringAlignment.Center,
                Alignment = StringAlignment.Center
            };

            RectangleF rectf = new(rect.Left, rect.Top, rect.Width, rect.Height);

            gfx.DrawString("Calculating time spread view...", Font, fgBrush, rectf, format);
        }

        BeginInvoke(new MethodInvoker(Refresh));
    }

    private void OnTimeSpreadingControlSizeChanged (object sender, EventArgs e)
    {
        if (TimeSpreadCalc != null)
        {
            _displayHeight = ClientRectangle.Height - (_edgeOffset * 3);
            TimeSpreadCalc.SetDisplayHeight(_displayHeight);
        }
    }

    private void OnTimeSpreadingControlMouseDown (object sender, MouseEventArgs e)
    {
    }

    private void OnTimeSpreadingControlMouseUp (object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            var entry = GetEntryForMouse(e);
            if (entry == null)
            {
                return;
            }

            OnLineSelected(new SelectLineEventArgs(entry.LineNum));
        }
    }

    private void OnTimeSpreadingControlMouseEnter (object sender, EventArgs e)
    {
        _toolTip.Active = true;
    }

    private void OnTimeSpreadingControlMouseLeave (object sender, EventArgs e)
    {
        _toolTip.Active = false;
    }

    private void OnTimeSpreadingControlMouseMove (object sender, MouseEventArgs e)
    {
        if (e.Y == _lastMouseY)
        {
            return;
        }

        if (e.Button == MouseButtons.Right)
        {
            DragContrast(e);
            return;
        }

        var entry = GetEntryForMouse(e);

        if (entry == null)
        {
            return;
        }

        _lastMouseY = e.Y;
        var dts = $"{entry.Timestamp:dd.MM.yyyy HH:mm:ss}";
        _toolTip.SetToolTip(this, "Line " + (entry.LineNum + 1) + "\n" + dts);
    }

    #endregion
}