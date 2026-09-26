namespace LogExpert.Core.Classes.Highlight;

/// <summary>
/// Immutable mapping between the rows of a Log Window's main grid and the original logical lines of its
/// Logfile Reader, after highlight-based hiding. Memory is proportional to the number of hidden lines.
/// <para>
/// <see cref="Append"/> shares storage with the map it extends, so appending tail lines is cheap. The storage is
/// append-only: a map only ever reads the prefix it was created with, so an older map stays valid.
/// </para>
/// </summary>
public sealed class LineVisibilityMap
{
    private readonly HiddenLineStore _store;

    private LineVisibilityMap (HiddenLineStore store, int lineCount, int hiddenCount)
    {
        _store = store;
        LineCount = lineCount;
        HiddenCount = hiddenCount;
    }

    /// <summary>A new map covering no lines. Not a shared instance, so unrelated appends never share storage.</summary>
    public static LineVisibilityMap Empty => Identity(0);

    /// <summary>Total number of original logical lines covered by the map.</summary>
    public int LineCount { get; }

    public int HiddenCount { get; }

    public int VisibleCount => LineCount - HiddenCount;

    /// <summary>A map in which every line is visible.</summary>
    public static LineVisibilityMap Identity (int lineCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineCount);
        return new LineVisibilityMap(new HiddenLineStore([], 0, lineCount), lineCount, 0);
    }

    /// <summary>Returns the original line displayed in <paramref name="row"/>, or -1 when there is no such row.</summary>
    public int RowToLine (int row)
    {
        if (row < 0 || row >= VisibleCount)
        {
            return -1;
        }

        if (HiddenCount == 0)
        {
            return row;
        }

        // hidden[i] - i is non-decreasing: count the hidden lines that lie before the row's line.
        var items = _store.Items;
        int low = 0, high = HiddenCount;
        while (low < high)
        {
            var mid = (low + high) >>> 1;
            if (items[mid] - mid <= row)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return row + low;
    }

    /// <summary>Returns the row displaying <paramref name="line"/>, or -1 when the line is hidden or out of range.</summary>
    public int LineToRow (int line)
    {
        if (line < 0 || line >= LineCount)
        {
            return -1;
        }

        var index = Array.BinarySearch(_store.Items, 0, HiddenCount, line);
        return index >= 0 ? -1 : line - ~index;
    }

    public bool IsHidden (int line)
    {
        return line >= 0 && line < LineCount && Array.BinarySearch(_store.Items, 0, HiddenCount, line) >= 0;
    }

    /// <summary>
    /// Returns the row of <paramref name="line"/> when it is visible; otherwise the row of the next visible line,
    /// falling back to the previous one. Returns -1 when no line is visible.
    /// </summary>
    public int NearestRow (int line)
    {
        if (VisibleCount == 0)
        {
            return -1;
        }

        if (line >= LineCount)
        {
            return VisibleCount - 1;
        }

        line = Math.Max(line, 0);
        var index = Array.BinarySearch(_store.Items, 0, HiddenCount, line);
        var hiddenBefore = index >= 0 ? index : ~index;

        // For a hidden line, line - hiddenBefore is the row of the next visible line, if there is one.
        return Math.Min(line - hiddenBefore, VisibleCount - 1);
    }

    /// <summary>
    /// Returns a map covering <paramref name="lineCount"/> lines, in which <paramref name="hiddenLines"/>
    /// (ascending, all within the appended range) are hidden in addition to this map's hidden lines.
    /// </summary>
    public LineVisibilityMap Append (int lineCount, IReadOnlyList<int> hiddenLines)
    {
        ArgumentNullException.ThrowIfNull(hiddenLines);
        ArgumentOutOfRangeException.ThrowIfLessThan(lineCount, LineCount);

        var previous = LineCount - 1;
        // Hidden lines must be ascending and within the appended range.
        foreach (var line in hiddenLines)
        {
            if (line <= previous || line >= lineCount)
            {
                throw new ArgumentOutOfRangeException(nameof(hiddenLines), line, null);
            }

            previous = line;
        }

        var store = _store.TryAppend(LineCount, HiddenCount, lineCount, hiddenLines)
            ?? HiddenLineStore.CopyAppend(_store.Items, HiddenCount, lineCount, hiddenLines);

        return new LineVisibilityMap(store, lineCount, HiddenCount + hiddenLines.Count);
    }

    /// <summary>Drops the first <paramref name="offset"/> lines (rollover) and renumbers the rest.</summary>
    public LineVisibilityMap Shift (int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);

        var items = _store.Items;
        var first = Array.BinarySearch(items, 0, HiddenCount, offset);
        first = first >= 0 ? first : ~first;

        var shifted = new int[HiddenCount - first];
        for (var i = 0; i < shifted.Length; i++)
        {
            shifted[i] = items[first + i] - offset;
        }

        var lineCount = Math.Max(0, LineCount - offset);
        return new LineVisibilityMap(new HiddenLineStore(shifted, shifted.Length, lineCount), lineCount, shifted.Length);
    }

    /// <summary>Keeps only the first <paramref name="lineCount"/> lines.</summary>
    public LineVisibilityMap Truncate (int lineCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(lineCount);

        lineCount = Math.Min(lineCount, LineCount);
        var kept = Array.BinarySearch(_store.Items, 0, HiddenCount, lineCount);
        kept = kept >= 0 ? kept : ~kept;

        var items = _store.Items.AsSpan(0, kept).ToArray();
        return new LineVisibilityMap(new HiddenLineStore(items, kept, lineCount), lineCount, kept);
    }

    /// <summary>
    /// True when this map extends <paramref name="other"/> without changing any of its lines, so rows that
    /// <paramref name="other"/> displays keep their row index.
    /// </summary>
    public bool IsAppendOf (LineVisibilityMap other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return ReferenceEquals(_store, other._store) && other.LineCount <= LineCount && other.HiddenCount <= HiddenCount;
    }

    /// <summary>
    /// Append-only storage of hidden line numbers shared by a chain of appended maps. It is only extended in place
    /// by the map at its committed end; any other append copies, so no map ever observes a changed prefix.
    /// </summary>
    private sealed class HiddenLineStore (int[] items, int committedHidden, int committedLines)
    {
        private readonly Lock _lock = new();
        private int _committedHidden = committedHidden;
        private int _committedLines = committedLines;
        private volatile int[] _items = items;

        public int[] Items => _items;

        public HiddenLineStore? TryAppend (int fromLines, int fromHidden, int lineCount, IReadOnlyList<int> hiddenLines)
        {
            lock (_lock)
            {
                if (_committedLines != fromLines || _committedHidden != fromHidden)
                {
                    return null;
                }

                var required = fromHidden + hiddenLines.Count;
                var target = _items;
                if (required > target.Length)
                {
                    target = new int[Math.Max(required, Math.Max(16, target.Length * 2))];
                    Array.Copy(_items, target, fromHidden);
                }

                for (var i = 0; i < hiddenLines.Count; i++)
                {
                    target[fromHidden + i] = hiddenLines[i];
                }

                _items = target;
                _committedHidden = required;
                _committedLines = lineCount;
                return this;
            }
        }

        public static HiddenLineStore CopyAppend (int[] items, int hiddenCount, int lineCount, IReadOnlyList<int> hiddenLines)
        {
            var copy = new int[hiddenCount + hiddenLines.Count];
            Array.Copy(items, copy, hiddenCount);
            for (var i = 0; i < hiddenLines.Count; i++)
            {
                copy[hiddenCount + i] = hiddenLines[i];
            }

            return new HiddenLineStore(copy, copy.Length, lineCount);
        }
    }
}
