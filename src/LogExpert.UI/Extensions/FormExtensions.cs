using System.Runtime.Versioning;

namespace LogExpert.UI.Extensions;
internal static class FormExtensions
{
    /// <summary>
    /// Enumerates all controls within the specified parent control, including nested child controls.
    /// </summary>
    /// <param name="parent">The parent control whose child controls are to be enumerated. Cannot be <see langword="null"/>.</param>
    /// <returns>An <see cref="IEnumerable{Control}"/> of <see cref="Control"/> objects representing all controls within the parent,
    /// including nested children.</returns>
    [SupportedOSPlatform("windows")]
    public static IEnumerable<Control> ControlsRecursive (this Control parent)
    {
        ArgumentNullException.ThrowIfNull(parent, nameof(parent));

        foreach (Control control in parent.Controls)
        {
            yield return control;

            // recurse into children
            foreach (var child in ControlsRecursive(control))
            {
                yield return child;
            }
        }
    }
}
