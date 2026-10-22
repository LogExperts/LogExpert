using LogExpert.Core.Config;

namespace LogExpert.UI.ControlCharDisplay;

/// <summary>
/// Pure builder for the clipboard string when copying a selection from a log cell.
/// Returns the raw substring by default; when both <see cref="ControlCharSettings.Substitute"/>
/// and <see cref="ControlCharSettings.CopyDisplayedForm"/> are true, returns the
/// substituted (displayed) form.
/// </summary>
internal static class SubstitutedClipboardBuilder
{
    public static string Build (
        ReadOnlySpan<char> rawText,
        int selectionStart,
        int selectionLength,
        ControlCharSettings settings)
    {
        if (selectionLength <= 0)
        {
            return string.Empty;
        }

        var slice = rawText.Slice(selectionStart, selectionLength);

        if (settings is null || !settings.Substitute || !settings.CopyDisplayedForm)
        {
            return slice.ToString();
        }

        // Substitution path: render the selected slice and concatenate.
        var rendered = ControlCharRenderer.Render(slice.ToString(), settings);
        if (rendered.Count == 1 && !rendered[0].IsSubstituted)
        {
            return rendered[0].RenderedText;
        }

        var sb = new System.Text.StringBuilder(slice.Length);
        foreach (var seg in rendered)
        {
            _ = sb.Append(seg.RenderedText);
        }

        return sb.ToString();
    }
}