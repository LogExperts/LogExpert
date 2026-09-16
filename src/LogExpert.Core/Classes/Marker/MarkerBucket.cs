namespace LogExpert.Core.Classes.Marker;

/// <summary>A populated vertical pixel, including its logical range and navigation target.</summary>
public readonly record struct MarkerBucket (int Pixel, int FirstLine, int LastLine, int Count, int TargetLine, int ColorArgb)
{
    /// <summary>Aggregates a line-ordered index without sampling; duplicate lines count only once.</summary>
    public static IReadOnlyList<MarkerBucket> Aggregate (IEnumerable<MarkerLine> matches, int lineCount, int height)
    {
        ArgumentNullException.ThrowIfNull(matches);
        if (lineCount <= 0 || height <= 0)
        {
            return [];
        }

        var buckets = new MarkerBucket[height];
        var priorities = new int[height];
        Array.Fill(priorities, int.MaxValue);
        var previousLine = -1;
        foreach (var match in matches)
        {
            var line = match.LineNumber;
            if (line < 0 || line >= lineCount)
            {
                continue;
            }

            var pixel = lineCount == 1 ? 0 : (int)((long)line * (height - 1) / (lineCount - 1));
            var bucket = buckets[pixel];
            var first = height == 1 ? 0 : (int)(((long)pixel * (lineCount - 1) + height - 2) / (height - 1));
            var last = height == 1 || pixel == height - 1 || lineCount == 1 ? lineCount - 1
                : (int)(((long)(pixel + 1) * (lineCount - 1) + height - 2) / (height - 1)) - 1;
            var target = bucket.Count == 0 ? line : bucket.TargetLine;
            var distance = Math.Abs(2L * line - first - last);
            var targetDistance = Math.Abs(2L * target - first - last);
            if (distance < targetDistance || (distance == targetDistance && line < target))
            {
                target = line;
            }

            var color = bucket.ColorArgb;
            if (match.Priority < priorities[pixel])
            {
                priorities[pixel] = match.Priority;
                color = match.ColorArgb;
            }

            buckets[pixel] = new MarkerBucket(pixel, first, last,
                bucket.Count + (line == previousLine ? 0 : 1), target, color);
            previousLine = line;
        }

        return buckets.Where(bucket => bucket.Count > 0).ToArray();
    }
}