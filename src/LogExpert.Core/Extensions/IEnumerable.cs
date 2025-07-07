namespace LogExpert.Core.Extensions;

public static class Extensions
{
    public static bool IsEmpty<T>(this IEnumerable<T> collection)
    {
        if (collection == null)
        {
            return true;
        }

        return !collection.Any();
    }

    public static bool IsEmpty<T>(this IList<T> list)
    {
        if (list == null)
        {
            return true;
        }

        return list.Count == 0;
    }
}