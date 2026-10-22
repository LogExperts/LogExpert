using System;
using System.Text;

using LogExpert.Core.Helpers;

using Newtonsoft.Json;

namespace LogExpert.Core.Classes.JsonConverters;

/// <summary>
/// Custom JsonConverter for Encoding objects.
/// Serializes the encoding as its name (e.g. "utf-8").
/// </summary>
public class EncodingJsonConverter : JsonConverter
{
    public override bool CanConvert (Type objectType)
    {
        return typeof(Encoding).IsAssignableFrom(objectType);
    }

    /// <summary>
    /// Serializes the Encoding object to its name.
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="serializer"></param>
    public override void WriteJson (JsonWriter writer, object? value, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is not Encoding encoding)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteValue(encoding.WebName);
    }

    /// <summary>
    /// Reads a JSON value and converts it to an <see cref="Encoding"/> object.
    /// </summary>
    /// <param name="reader">The <see cref="JsonReader"/> to read from. Cannot be <see langword="null"/>.</param>
    /// <param name="objectType">The type of the object to deserialize. This parameter is not used in this method.</param>
    /// <param name="existingValue">The existing value of the object being deserialized. This parameter is not used in this method.</param>
    /// <param name="serializer">The calling serializer. This parameter is not used in this method.</param>
    /// <returns>An <see cref="Encoding"/> object corresponding to the JSON value.  Returns <see cref="Encoding.Default"/> if the
    /// JSON value is <see langword="null"/>, empty, or an invalid encoding name.</returns>
    public override object? ReadJson (JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        return EncodingRegistry.GetEncoding(reader.Value?.ToString(), Encoding.Default);
    }
}
