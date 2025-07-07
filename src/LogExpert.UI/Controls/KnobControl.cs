using NLog;

using System.Globalization;
using System.Runtime.Versioning;

namespace LogExpert.UI.Controls;

[SupportedOSPlatform("windows")]
internal partial class KnobControl : UserControl
{
    #region Fields

    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();


    private readonly StringFormat _stringFormat = new();

    private bool _isShiftPressed;

    private int _oldValue;
    private int _startMouseY;
    private int _value;

    #endregion

    #region cTor

    public KnobControl()
    {
        InitializeComponent();
        _stringFormat.LineAlignment = StringAlignment.Far;
        _stringFormat.Alignment = StringAlignment.Center;
    }

    #endregion

    #region Delegates

    public delegate void ValueChangedEventHandler(object sender, EventArgs e);

    #endregion

    #region Events

    public event ValueChangedEventHandler ValueChanged;

    #endregion

    #region Properties

    public int MinValue { get; set; }

    public int MaxValue { get; set; }

    public int Value
    {
        get => _value;
        set
        {
            _value = value;
            Refresh();
        }
    }


    public int Range => MaxValue - MinValue;

    public int DragSensitivity { get; set; } = 3;

    #endregion

    #region Overrides

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        Color foregroundColor = Enabled ? Color.Black : Color.Gray;

        Pen blackPen = new(foregroundColor, 1);
        Pen greyPen = new(Color.Gray, 1);

        Rectangle rect = ClientRectangle;
        var height = Font.Height + 3;
        if (height > rect.Height)
        {
            height = rect.Height + 3;
        }
        rect.Inflate(-1, -height / 2);
        rect.Offset(0, -height / 2);
        e.Graphics.DrawEllipse(greyPen, rect);

        //rect = this.ClientRectangle;
        rect.Inflate(-2, -2);

        var startAngle = 135.0F + 270F * ((float)_value / (float)Range);
        var sweepAngle = 0.1F;
        e.Graphics.DrawPie(blackPen, rect, startAngle, sweepAngle);

        Brush brush = new SolidBrush(foregroundColor);
        RectangleF rectF = new(0, 0, ClientRectangle.Width, ClientRectangle.Height);
        e.Graphics.DrawString("" + _value, Font, brush, rectF, _stringFormat);

        blackPen.Dispose();
        greyPen.Dispose();
        brush.Dispose();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            Capture = true;
            _startMouseY = e.Y;
            _oldValue = Value;
        }
        if (e.Button == MouseButtons.Right)
        {
            Capture = false;
            Value = _oldValue;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        Capture = false;
        _oldValue = Value;
        OnValueChanged(new EventArgs());
    }

    protected void OnValueChanged(EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (!Capture)
        {
            return;
        }

        var sense = _isShiftPressed ? DragSensitivity * 2 : DragSensitivity;

        var diff = _startMouseY - e.Y;
        _logger.Debug(CultureInfo.InvariantCulture, "KnobDiff: {0}", diff);
        var range = MaxValue - MinValue;
        _value = _oldValue + diff / sense;

        if (_value < MinValue)
        {
            _value = MinValue;
        }

        if (_value > MaxValue)
        {
            _value = MaxValue;
        }
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        _isShiftPressed = e.Shift;
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        _isShiftPressed = e.Shift;
        base.OnKeyUp(e);
    }

    #endregion
}