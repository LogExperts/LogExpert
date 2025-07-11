using System.Reflection;
using System.Runtime.Versioning;

using LogExpert.UI.Extensions;

namespace LogExpert.Extensions;
internal class ResourceHelper
{
    /// <summary>
    /// Generates a mapping of controls to their corresponding text values based on resource naming conventions.
    /// </summary>
    /// <remarks>The method constructs resource keys using the format
    /// "{className}_{rescourceMainType}_{ControlType}_{ControlName}" and retrieves the associated text from the
    /// resources. Only controls with matching resource entries will be included in the returned dictionary.</remarks>
    /// <param name="form">The form containing the controls to be mapped.</param>
    /// <param name="className">The class name used as part of the resource key.</param>
    /// <param name="rescourceMainType">The main type of the resource used in the resource key (UI, Logger, etc).</param>
    /// <returns>A dictionary where each key is a <see cref="Control"/> from the form, and each value is the text associated with
    /// that control, as defined in the resources. The dictionary will only include controls for which a corresponding
    /// resource text is found.</returns>
    [SupportedOSPlatform("windows")]
    public static Dictionary<Control, string> GenerateTextMapFromNaming (Form form, string className, string rescourceMainType)
    {
        var map = new Dictionary<Control, string>();
        var resourcesType = typeof(Resources);
        var resourceProperties = resourcesType.GetProperties(BindingFlags.Public | BindingFlags.Static);
        var controls = form.ControlsRecursive();

        foreach (var control in controls)
        {
            var type = control.GetType();
            var resourceKey = $"{className}_{rescourceMainType}_{control.GetType().Name}_{control.Name}";
            var prop = resourceProperties.FirstOrDefault(p => p.Name == resourceKey);
            if (prop != null)
            {
                var value = prop.GetValue(null) as string;
                if (!string.IsNullOrEmpty(value))
                {
                    map[control] = value;
                }
            }
        }

        return map;
    }
}
