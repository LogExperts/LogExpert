using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

namespace LogExpert.UI.Controls;

[SupportedOSPlatform("windows")]
internal class ColorComboBox : ComboBox
{
    #region Fields

    private Color _customColor = Color.FromKnownColor(KnownColor.Black);

    #endregion

    #region cTor

    public ColorComboBox ()
    {
        DrawMode = DrawMode.OwnerDrawFixed;
        DrawItem += OnColorComboBoxDrawItem;
        if (!DesignMode)
        {
            Items.AddRange(
                [
                    _customColor,
                    Color.Black,
                    Color.White,
                    Color.Gray,
                    Color.DarkGray,
                    Color.Blue,
                    Color.LightBlue,
                    Color.DarkBlue,
                    Color.Green,
                    Color.LightGreen,
                    Color.DarkGreen,
                    Color.Olive,
                    Color.Red,
                    Color.Pink,
                    Color.Purple,
                    Color.IndianRed,
                    Color.DarkCyan,
                    Color.Yellow
                ]
            );
        }
    }

    #endregion

    #region Properties

    public Color CustomColor
    {
        get => _customColor;
        set
        {
            _customColor = value;
            Items.RemoveAt(0);
            Items.Insert(0, _customColor);
        }
    }

    public Color SelectedColor => (Color)(SelectedIndex != -1 ? Items[SelectedIndex] : null);

    #endregion

    #region Events handler

    private void OnColorComboBoxDrawItem (object sender, DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index >= 0)
        {
            Rectangle rectangle = new(4, e.Bounds.Top + 2, 30, e.Bounds.Height - 4);
            var rectColor = (Color)Items[e.Index];
            e.Graphics.FillRectangle(new SolidBrush(rectColor), rectangle);
            e.Graphics.DrawRectangle(Pens.Black, rectangle);

            if (e.Index == 0)
            {
                e.Graphics.DrawString("Custom", e.Font, Brushes.Black,
                    new PointF(42, e.Bounds.Top + 2));
            }
            else
            {
                e.Graphics.DrawString(((Color)Items[e.Index]).Name, e.Font, Brushes.Black,
                    new PointF(42, e.Bounds.Top + 2));
            }

            if (!Enabled)
            {
                HatchBrush brush = new(HatchStyle.Percent50, Color.LightGray, Color.FromArgb(10, Color.LightGray));
                rectangle.Inflate(1, 1);
                e.Graphics.FillRectangle(brush, rectangle);
                brush.Dispose();
            }
            e.DrawFocusRectangle();
        }
    }

    #endregion
}