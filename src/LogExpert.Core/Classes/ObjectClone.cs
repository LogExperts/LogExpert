using System.IO;
using System.Text.Json;

namespace LogExpert.Core.Classes;

public static class ObjectClone
{
    #region Public methods

    public static T Clone<T>(T RealObject)
    {
        using MemoryStream objectStream = new();

        JsonSerializer.Serialize(objectStream, RealObject);
        objectStream.Seek(0, SeekOrigin.Begin);
        return JsonSerializer.Deserialize<T>(objectStream);
    }

    #endregion
}