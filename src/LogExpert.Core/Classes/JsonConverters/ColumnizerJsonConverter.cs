using System.Reflection;

using ColumnizerLib;

using LogExpert.Core.Classes.Attributes;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogExpert.Core.Classes.JsonConverters;

/// <summary>
/// Custom JsonConverter for ILogLineColumnizer implementations.
/// Serializes only properties marked with [JsonColumnizerProperty].
/// Uses AssemblyQualifiedName for reliable type identification and GetName() for display purposes.
/// </summary>
public class ColumnizerJsonConverter : JsonConverter
{
    public override bool CanConvert (Type objectType)
    {
        return typeof(ILogLineMemoryColumnizer).IsAssignableFrom(objectType);
    }

    public override void WriteJson (JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(value);

        if (value is not ILogLineMemoryColumnizer columnizer)
        {
            writer.WriteNull();
            return;
        }

        var type = value.GetType();
        var stateObj = new JObject();
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<JsonColumnizerPropertyAttribute>() != null && prop.CanRead)
            {
                var propValue = prop.GetValue(value);
                stateObj[prop.Name] = propValue != null ? JToken.FromObject(propValue, serializer) : JValue.CreateNull();
            }
        }

        writer.WriteStartObject();
        writer.WritePropertyName("TypeName");
        writer.WriteValue(type.AssemblyQualifiedName);
        writer.WritePropertyName("DisplayName");
        writer.WriteValue(columnizer.GetName());
        writer.WritePropertyName("CustomName");
        writer.WriteValue(columnizer.GetCustomName());
        writer.WritePropertyName("State");
        stateObj.WriteTo(writer);
        writer.WriteEndObject();
    }

    public override object ReadJson (JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var jObject = JObject.Load(reader);

        // Try new format first (TypeName)
        var typeName = jObject["TypeName"]?.ToString();

        // Fall back to old format (Type) for backward compatibility
        if (string.IsNullOrEmpty(typeName))
        {
            var displayName = jObject["Type"]?.ToString();
            if (!string.IsNullOrEmpty(displayName))
            {
                // Try to find by display name (old behavior)
                var foundType = FindColumnizerTypeByName(displayName);
                if (foundType != null)
                {
                    typeName = foundType.AssemblyQualifiedName;
                }
            }
        }

        if (string.IsNullOrEmpty(typeName) || jObject["State"] is not JObject state)
        {
            return null;
        }

        // Load the type
        Type columnizerType;
        try
        {
            columnizerType = Type.GetType(typeName, throwOnError: false);
            //FindColumnizerTypeByAssemblyQualifiedName(typeName);

            // If Type.GetType fails or finds something that is not a columnizer, try finding it manually
            if (columnizerType == null ||
                !typeof(ILogLineMemoryColumnizer).IsAssignableFrom(columnizerType) ||
                columnizerType.IsInterface ||
                columnizerType.IsAbstract)
            {
                columnizerType = FindColumnizerTypeByAssemblyQualifiedName(typeName);
            }

            if (columnizerType == null)
            {
                throw new JsonSerializationException($"Columnizer type '{typeName}' could not be loaded.");
            }
        }
        catch (Exception ex) when (ex is ArgumentException or
                                        FileNotFoundException or
                                        FileLoadException or
                                        BadImageFormatException)
        {
            throw new JsonSerializationException($"Failed to load columnizer type '{typeName}'.", ex);
        }

        var instance = Activator.CreateInstance(columnizerType);

        // Restore state properties
        foreach (var prop in columnizerType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (prop.GetCustomAttribute<JsonColumnizerPropertyAttribute>() != null && prop.CanWrite)
            {
                var token = state[prop.Name];
                if (token != null && token.Type != JTokenType.Null)
                {
                    var value = token.ToObject(prop.PropertyType, serializer);
                    prop.SetValue(instance, value);
                }
            }
        }

        return instance;
    }

    private static Type FindColumnizerTypeByName (string name)
    {
        // Search all loaded assemblies for a type implementing ILogLineColumnizer with matching GetName()

        foreach (var currentAssembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in currentAssembly.GetTypes().Where(t => typeof(ILogLineMemoryColumnizer).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
            {
                try
                {
                    // First check if the type name matches (e.g., "Regex1Columnizer")
                    if (type.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return type;
                    }

                    // Then check if the GetName() matches (e.g., "Regex1")
                    if (Activator.CreateInstance(type) is ILogLineMemoryColumnizer instance && instance.GetName() == name)
                    {
                        return type;
                    }
                }
                catch (Exception ex) when (ex is ArgumentNullException or
                                           ArgumentException or
                                           NotSupportedException or
                                           TargetInvocationException or
                                           MethodAccessException or
                                           MemberAccessException or
                                           MissingMethodException or
                                           TypeLoadException)
                {
                    // intentionally ignored
                }
            }
        }

        return null;
    }

    private static Type FindColumnizerTypeByAssemblyQualifiedName (string assemblyQualifiedName)
    {
        // Extract the type name without version/culture/token for more flexible matching
        var typeNameParts = assemblyQualifiedName.Split(',');
        if (typeNameParts.Length < 2)
        {
            return null;
        }

        var typeName = typeNameParts[0].Trim();
        var assemblyName = typeNameParts[1].Trim();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(a => a.GetName().Name == assemblyName))
        {
            var type = assembly.GetType(typeName);
            if (type != null && typeof(ILogLineMemoryColumnizer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            {
                return type;
            }
        }

        // Fallback: try to find by simple type name (without namespace) across all assemblies
        var simpleTypeName = typeName.Contains('.', StringComparison.OrdinalIgnoreCase) ? typeName[(typeName.LastIndexOf('.') + 1)..] : typeName;

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes().Where(t =>
                typeof(ILogLineMemoryColumnizer).IsAssignableFrom(t) &&
                !t.IsInterface &&
                !t.IsAbstract &&
                t.Name.Equals(simpleTypeName, StringComparison.OrdinalIgnoreCase)))
            {
                return type;
            }
        }

        return null;
    }
}