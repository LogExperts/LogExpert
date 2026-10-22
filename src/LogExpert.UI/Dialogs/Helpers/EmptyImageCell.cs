namespace LogExpert.UI.Dialogs.Helpers;

/// <summary>
/// Image cell whose new-row default is a blank bitmap instead of the framework's "image in error"
/// glyph, so the columnizer grid's new-row placeholder does not show a red X.
/// </summary>
public sealed class EmptyImageCell : DataGridViewImageCell
{
    private static readonly Image _blank = new Bitmap(16, 16);

    public override object DefaultNewRowValue => _blank;
}