using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.Versioning;

using LogExpert.Core.Classes.DateTimeParser;
using LogExpert.Core.Enums;

namespace LogExpert.Dialogs;

/// <summary>
/// This control displays date and time and allows user to interact with the individual parts using the mouse
/// to increment and decrement the values. The date format displayed is derived from the application UI locale.
/// We currently support only three: US (mm/dd/yyyy), French (yyyy-mm-dd) and German (dd.mm.yyyy).
/// The control raises events (ValueChanged, ValueDragged) when the date/time changes so that owner can react accordingly.
/// </summary>
[SupportedOSPlatform("windows")]
internal partial class DateTimeDragControl : UserControl
{
    #region Fields

    private const int NO_DIGIT_DRAGGED = -1;
    private int _addedValue;


    private DateTime _dateTime;
    private readonly IList<Rectangle> _digitRects = [];

    private readonly StringFormat _digitsFormat = new();
    private int _draggedDigit;

    private DragOrientationsEnum _dragOrientation = DragOrientationsEnum.Vertical;

    private readonly ToolStripItem toolStripItemHorizontalDrag = new ToolStripMenuItem();
    private readonly ToolStripItem toolStripItemVerticalDrag = new ToolStripMenuItem();
    private readonly ToolStripItem toolStripItemVerticalInvertedDrag = new ToolStripMenuItem();

    private string[] _dateParts;

    private int _startMouseX;
    private int _startMouseY;

    #endregion

    #region cTor

    public DateTimeDragControl ()
    {
        InitializeComponent();

        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;

        _digitsFormat.LineAlignment = StringAlignment.Center;
        _digitsFormat.Alignment = StringAlignment.Near;
        _digitsFormat.Trimming = StringTrimming.None;
        _digitsFormat.FormatFlags = StringFormatFlags.FitBlackBox | StringFormatFlags.NoClip | StringFormatFlags.NoWrap;

        _draggedDigit = NO_DIGIT_DRAGGED;
    }

    #endregion

    #region Events

    public event EventHandler<EventArgs> ValueChanged;
    public event EventHandler<EventArgs> ValueDragged;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the minimum allowable date and time value.
    /// </summary>
    public DateTime MinDateTime { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the maximum allowable date and time value.
    /// </summary>
    public DateTime MaxDateTime { get; set; } = DateTime.MaxValue;

    /// <summary>
    /// Gets or sets the orientation for drag operations.
    /// </summary>
    public DragOrientationsEnum DragOrientation
    {
        get => _dragOrientation;
        set
        {
            _dragOrientation = value;
            UpdateContextMenu();
        }
    }

    /// <summary>
    /// Gets or sets the color used to highlight an element when the mouse hovers over it.
    /// </summary>
    public Color HoverColor { get; set; }

    /// <summary>
    /// Gets or sets the date and time value, adjusted to exclude milliseconds.
    /// </summary>
    public DateTime DateTime
    {
        get => _dateTime.Subtract(TimeSpan.FromMilliseconds(_dateTime.Millisecond));
        set
        {
            _dateTime = value;

            if (_dateTime < MinDateTime)
            {
                _dateTime = MinDateTime;
            }

            if (_dateTime > MaxDateTime)
            {
                _dateTime = MaxDateTime;
            }
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Determines the index of the digit rectangle under the mouse cursor.
    /// </summary>
    /// <param name="e">The mouse event arguments containing the location of the cursor.</param>
    /// <returns>The index of the rectangle in <c>_digitRects</c> that contains the cursor location and corresponds to a date
    /// part; otherwise, returns <c>NO_DIGIT_DRAGGED</c> if no such rectangle is found.</returns>
    private int DetermineDraggedDigit (MouseEventArgs e)
    {
        for (var i = 0; i < _digitRects.Count; ++i)
        {
            if (_digitRects[i].Contains(e.Location) && Token.IsDatePart(_dateParts[i]))
            {
                return i;
            }
        }

        return NO_DIGIT_DRAGGED;
    }

    /// <summary>
    /// Retrieves the value of the date or time component currently being dragged.
    /// </summary>
    /// <returns>The integer value of the date or time component corresponding to the dragged digit. Returns the year, month,
    /// day, hour, minute, or second based on the dragged component. If no valid component is being dragged, returns a
    /// sentinel value indicating no digit is dragged.</returns>
    private int GetDraggedValue ()
    {
        var datePart = _dateParts[_draggedDigit];

        return datePart.StartsWith('y')
            ? _dateTime.Year
            : datePart.StartsWith('M')
                ? _dateTime.Month
                : datePart.StartsWith('d')
                    ? _dateTime.Day
                    : datePart.StartsWith('h')
                        ? _dateTime.Hour
                        : datePart.StartsWith('m')
                            ? _dateTime.Minute
                            : datePart.StartsWith('s')
                                ? _dateTime.Second
                                : NO_DIGIT_DRAGGED;
    }

    /// <summary>
    /// Adjusts the current date and time by a specified delta based on the dragged digit.
    /// </summary>
    /// <remarks>The adjustment is applied to the date part corresponding to the currently dragged digit,
    /// which can be a year, month, day, hour, minute, or second. If the resulting date and time exceed the defined
    /// <c>MaxDateTime</c> or fall below <c>MinDateTime</c>, the date and time are clamped to these limits, and the
    /// method returns <see langword="false"/>.</remarks>
    /// <param name="delta">The amount by which to adjust the date and time. Positive values increase the date/time, while negative values
    /// decrease it.</param>
    /// <returns><see langword="true"/> if the date and time were successfully adjusted within the allowed range; otherwise, <see
    /// langword="false"/>.</returns>
    private bool SetDraggedValue (int delta)
    {
        if (_draggedDigit == NO_DIGIT_DRAGGED)
        {
            return false;
        }

        var changed = true;
        try
        {
            var datePart = _dateParts[_draggedDigit];

            if (datePart.StartsWith('y'))
            {
                _dateTime = _dateTime.AddYears(delta);
            }
            else if (datePart.StartsWith('M'))
            {
                _dateTime = _dateTime.AddMonths(delta);
            }
            else if (datePart.StartsWith('d'))
            {
                _dateTime = _dateTime.AddDays(delta);
            }
            else if (datePart.StartsWith('h'))
            {
                _dateTime = _dateTime.AddHours(delta);
            }
            else if (datePart.StartsWith('m'))
            {
                _dateTime = _dateTime.AddMinutes(delta);
            }
            else if (datePart.StartsWith('s'))
            {
                _dateTime = _dateTime.AddSeconds(delta);
            }
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException)
        {
            // invalid value dragged
        }

        if (_dateTime > MaxDateTime)
        {
            _dateTime = MaxDateTime;
            changed = false;
        }

        if (_dateTime < MinDateTime)
        {
            _dateTime = MinDateTime;
            changed = false;
        }

        return changed;
    }

    /// <summary>
    /// Initializes custom rectangles for each part of the date section.
    /// </summary>
    /// <remarks>This method calculates the width of each date part based on the available client rectangle
    /// width and adjusts the rectangles accordingly. It clears any existing rectangles before initializing new
    /// ones.</remarks>
    /// <param name="dateSection">The section containing date parts to be formatted and measured.</param>
    private void InitCustomRects (Section dateSection)
    {
        _dateParts = [.. dateSection
            .GeneralTextDateDurationParts
            .Select(DateFormatPartAdjuster.AdjustDateTimeFormatPart)];

        var oneCharWidth = ClientRectangle.Width / _dateParts.Sum(s => s.Length);
        var left = ClientRectangle.Left;

        _digitRects.Clear();

        foreach (var datePart in _dateParts)
        {
            var s = datePart.Length * oneCharWidth;
            _digitRects.Add(new Rectangle(left, ClientRectangle.Top, s, ClientRectangle.Height));
            left += s;
        }
    }

    /// <summary>
    /// Initializes the digit rectangles based on the current culture's date and time format.
    /// </summary>
    /// <remarks>This method attempts to parse the current culture's short date and long time pattern to
    /// determine the sections for digit rectangles. If parsing fails, it defaults to a standard format of "dd.MM.yyyy
    /// HH:mm:ss".</remarks>
    private void InitDigitRects ()
    {
        var culture = CultureInfo.CurrentCulture;

        var datePattern = string.Concat(culture.DateTimeFormat.ShortDatePattern, " ", culture.DateTimeFormat.LongTimePattern);

        var sections = Parser.ParseSections(datePattern, out _);
        var dateSection = sections.FirstOrDefault();

        if (dateSection == null)
        {
            sections = Parser.ParseSections("dd.MM.yyyy HH:mm:ss", out var _);
            dateSection = sections.Single();
        }

        InitCustomRects(dateSection);
    }

    #endregion

    #region Events handler

    /// <summary>
    /// Handles the load event for the DateTime drag control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnDateTimeDragControlLoad (object sender, EventArgs e)
    {
        InitDigitRects();

        BuildContextualMenu();
    }

    #endregion

    /// <summary>
    /// Raises the <see cref="ValueChanged"/> event.
    /// </summary>
    /// <remarks>This method is called to notify subscribers that the value has changed. It invokes the <see
    /// cref="ValueChanged"/> event handler, if it is not null.</remarks>
    /// <param name="e">An <see cref="EventArgs"/> object that contains the event data.</param>
    protected void OnValueChanged (EventArgs e)
    {
        ValueChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Raises the <see cref="ValueDragged"/> event.
    /// </summary>
    /// <remarks>This method is called to notify subscribers that a value has been dragged. Override this
    /// method in a derived class to handle the event without attaching a delegate.</remarks>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    protected void OnValueDragged (EventArgs e)
    {
        ValueDragged?.Invoke(this, e);
    }

    #region Contextual Menu

    /// <summary>
    /// Builds and initializes the contextual menu for timestamp selection.
    /// </summary>
    /// <remarks>This method sets up the contextual menu with options for horizontal, vertical, and inverted
    /// vertical drag actions. It assigns click event handlers to each menu item and updates the menu with the current
    /// state.</remarks>
    private void BuildContextualMenu ()
    {
        ContextMenuStrip = new ContextMenuStrip
        {
            Name = Resources.DateTimeDragControl_UI_ContextMenuStrip_TimestampSelector
        };

        _ = ContextMenuStrip.Items.Add(toolStripItemHorizontalDrag);
        _ = ContextMenuStrip.Items.Add(toolStripItemVerticalDrag);
        _ = ContextMenuStrip.Items.Add(toolStripItemVerticalInvertedDrag);

        toolStripItemHorizontalDrag.Click += OnToolStripItemHorizontalDragClick;
        toolStripItemHorizontalDrag.Text = Resources.DateTimeDragControl_UI_ToolStripItem_toolStripItemHorizontalDrag;

        toolStripItemVerticalDrag.Click += OnToolStripItemVerticalDragClick;
        toolStripItemVerticalDrag.Text = Resources.DateTimeDragControl_UI_ToolStripItem_toolStripItemVerticalDrag;

        toolStripItemVerticalInvertedDrag.Click += OnToolStripItemVerticalInvertedDragClick;
        toolStripItemVerticalInvertedDrag.Text = Resources.DateTimeDragControl_UI_ToolStripItem_toolStripItemInvertedDrag;

        ContextMenuStrip.Opening += OnContextMenuStripOpening;

        UpdateContextMenu();
    }

    /// <summary>
    /// Updates the state of the context menu items based on the current drag orientation.
    /// </summary>
    /// <remarks>This method enables or disables specific context menu items to reflect the current drag
    /// orientation. The menu items are adjusted so that only the relevant drag options are enabled.</remarks>
    private void UpdateContextMenu ()
    {
        toolStripItemHorizontalDrag.Enabled = DragOrientation != DragOrientationsEnum.Horizontal;
        toolStripItemVerticalDrag.Enabled = DragOrientation != DragOrientationsEnum.Vertical;
        toolStripItemVerticalInvertedDrag.Enabled = DragOrientation != DragOrientationsEnum.InvertedVertical;
    }

    /// <summary>
    /// Handles the event when the context menu strip is about to open.
    /// </summary>
    /// <remarks>Cancels the opening of the context menu strip if the control is currently capturing
    /// input.</remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">A <see cref="CancelEventArgs"/> that contains the event data.</param>
    private void OnContextMenuStripOpening (object sender, CancelEventArgs e)
    {
        if (Capture)
        {
            e.Cancel = true;
        }
    }

    /// <summary>
    /// Handles the click event for the horizontal drag ToolStrip item, setting the drag orientation to horizontal.
    /// </summary>
    /// <remarks>This method disables the horizontal drag ToolStrip item and enables the vertical and vertical
    /// inverted drag ToolStrip items.</remarks>
    /// <param name="sender">The source of the event, typically the ToolStrip item that was clicked.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnToolStripItemHorizontalDragClick (object sender, EventArgs e)
    {
        DragOrientation = DragOrientationsEnum.Horizontal;
        toolStripItemHorizontalDrag.Enabled = false;
        toolStripItemVerticalDrag.Enabled = true;
        toolStripItemVerticalInvertedDrag.Enabled = true;
    }

    /// <summary>
    /// Handles the click event for enabling vertical drag orientation on a ToolStrip item.
    /// </summary>
    /// <param name="sender">The source of the event, typically the ToolStrip item that was clicked.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnToolStripItemVerticalDragClick (object sender, EventArgs e)
    {
        DragOrientation = DragOrientationsEnum.Vertical;
        toolStripItemHorizontalDrag.Enabled = true;
        toolStripItemVerticalDrag.Enabled = false;
        toolStripItemVerticalInvertedDrag.Enabled = true;
    }

    /// <summary>
    /// Handles the click event for the vertical inverted drag ToolStrip item.
    /// </summary>
    /// <remarks>This method sets the drag orientation to inverted vertical and updates the enabled state of
    /// related ToolStrip items.</remarks>
    /// <param name="sender">The source of the event, typically the ToolStrip item that was clicked.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnToolStripItemVerticalInvertedDragClick (object sender, EventArgs e)
    {
        DragOrientation = DragOrientationsEnum.InvertedVertical;
        toolStripItemHorizontalDrag.Enabled = true;
        toolStripItemVerticalDrag.Enabled = true;
        toolStripItemVerticalInvertedDrag.Enabled = false;
    }

    #endregion

    #region Rendering

    /// <summary>
    /// Handles the painting of the control, rendering the current date and time values with a user-defined format and
    /// highlighting any dragged digit.
    /// </summary>
    /// <remarks>This method overrides the base <see cref="Control.OnPaint"/> method to provide custom
    /// rendering logic. It highlights a dragged digit, if any, and displays the current date and time using a specified
    /// format. The method ensures that the date parts are formatted correctly and handles any exceptions that may arise
    /// from invalid date formats.</remarks>
    /// <param name="e">A <see cref="PaintEventArgs"/> that contains the event data.</param>
    protected override void OnPaint (PaintEventArgs e)
    {
        base.OnPaint(e);

        // Show what digit is dragged
        using (Brush hoverBrush = new SolidBrush(HoverColor))
        {
            if (_draggedDigit != NO_DIGIT_DRAGGED)
            {
                e.Graphics.FillRectangle(hoverBrush, _digitRects[_draggedDigit]);
            }
        }

        // Display current value with user-defined date format and fixed time format ("HH:mm:ss")
        using Brush brush = new SolidBrush(Color.Black);
        for (var i = 0; i < _dateParts.Length; i++)
        {
            var datePart = _dateParts[i];
            var rect = _digitRects[i];
            string value;

            if (Token.IsDatePart(datePart))
            {
                try
                {
                    value = _dateTime.ToString("-" + datePart + "-", CultureInfo.InvariantCulture);
                    value = value[1..^1];
                }
                catch (Exception ex) when (ex is FormatException
                                              or ArgumentOutOfRangeException)
                {
                    value = datePart;
                }
            }
            else
            {
                value = datePart;
            }

            e.Graphics.DrawString(value, Font, brush, rect, _digitsFormat);
        }
    }

    /// <summary>
    /// Handles the resize event of the DateTimeDragControl.
    /// </summary>
    /// <param name="sender">The source of the event, typically the control being resized.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnDateTimeDragControlResize (object sender, EventArgs e)
    {
        InitDigitRects();
    }

    #endregion

    #region Mouse callbacks

    /// <summary>
    /// Handles the mouse down event for the control, initiating a drag operation if the left mouse button is pressed.
    /// </summary>
    /// <remarks>If the left mouse button is pressed, the method determines which digit is being dragged and
    /// starts the drag operation. If the right mouse button is pressed while a drag operation is in progress, the drag
    /// is canceled and any changes are undone. The control is invalidated to trigger a repaint, reflecting the current
    /// state.</remarks>
    /// <param name="e">A <see cref="MouseEventArgs"/> that contains the event data.</param>
    protected override void OnMouseDown (MouseEventArgs e)
    {
        base.OnMouseDown(e);

        if (e.Button == MouseButtons.Left)
        {
            _draggedDigit = DetermineDraggedDigit(e);
            if (_draggedDigit == NO_DIGIT_DRAGGED)
            {
                return;
            }

            Capture = true;
            _startMouseY = e.Y;
            _startMouseX = e.X;
            _addedValue = 0;
        }
        else if (e.Button == MouseButtons.Right && Capture)
        {
            Capture = false;
            _ = SetDraggedValue(0); //undo
        }

        Invalidate(); // repaint with the selected item (or none)
    }

    /// <summary>
    /// Handles the mouse button release event for the control.
    /// </summary>
    /// <remarks>This method is called when the mouse button is released over the control. It stops capturing
    /// the mouse, resets the dragged digit state, and triggers a repaint of the control. It also raises the <see
    /// cref="OnValueChanged"/> event.</remarks>
    /// <param name="e">A <see cref="MouseEventArgs"/> that contains the event data.</param>
    protected override void OnMouseUp (MouseEventArgs e)
    {
        if (!Capture)
        {
            return;
        }

        base.OnMouseUp(e);

        Capture = false;
        _draggedDigit = NO_DIGIT_DRAGGED;
        Invalidate(); // repaint without the selected item

        OnValueChanged(EventArgs.Empty);
    }

    /// <summary>
    /// Handles the mouse move event to update the dragged value based on the mouse movement.
    /// </summary>
    /// <remarks>This method calculates the difference in mouse position based on the specified drag
    /// orientation and updates the dragged value accordingly. It only processes the event if the mouse capture is
    /// active. The method invalidates the control to trigger a repaint and raises the <c>OnValueDragged</c> event if
    /// the dragged value is successfully updated.</remarks>
    /// <param name="e">The <see cref="MouseEventArgs"/> containing the event data.</param>
    protected override void OnMouseMove (MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!Capture)
        {
            return;
        }

        int diff;
        switch (DragOrientation)
        {
            case DragOrientationsEnum.Vertical:
                {
                    diff = _startMouseY - e.Y;
                    break;
                }
            case DragOrientationsEnum.InvertedVertical:
                {
                    diff = _startMouseY + e.Y;
                    break;
                }
            case DragOrientationsEnum.Horizontal:
            default:
                {
                    diff = e.X - _startMouseX;
                    break;
                }
        }

        var delta = (diff / 5) - _addedValue; // one unit per 5 pixels move

        if (delta == 0)
        {
            return;
        }

        if (SetDraggedValue(delta))
        {
            _addedValue += delta;
        }

        Invalidate();

        OnValueDragged(EventArgs.Empty);
    }

    /// <summary>
    /// Handles the <see cref="MouseLeave"/> event for the date-time drag control.
    /// </summary>
    /// <remarks>Resets the dragged digit state and refreshes the control when the mouse leaves the control
    /// area, unless the control is currently capturing the mouse.</remarks>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
    private void OnDateTimeDragControlMouseLeave (object sender, EventArgs e)
    {
        if (Capture)
        {
            return;
        }

        _draggedDigit = NO_DIGIT_DRAGGED;
        Refresh();
    }

    #endregion
}