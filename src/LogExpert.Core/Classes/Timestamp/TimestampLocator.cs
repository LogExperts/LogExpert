namespace LogExpert.Core.Classes.Timestamp;

/// <summary>
/// Timestamp lookup over a Logfile Reader and the active Columnizer: what time is the line at N,
/// and which line carries time T.
/// </summary>
public sealed class TimestampLocator (ITimestampSource source)
{
    /// <summary>
    /// Gets the timestamp for the line at or before <paramref name="lineNum"/>. If that line has
    /// no timestamp, the previous line is checked, and so on, until one is found.
    /// </summary>
    /// <param name="lineNum">The line to start scanning backward from.</param>
    /// <param name="lineCount">The number of lines currently available (<c>ILogfileReader.LineCount</c>).</param>
    /// <param name="roundToSeconds">If true, the returned timestamp has its millisecond component zeroed.</param>
    /// <param name="token">Checked once per line; a cancelled token stops the scan and returns MinValue.</param>
    /// <returns>The timestamp found, or <see cref="DateTime.MinValue"/> if none was, and the line
    /// number it was found on (unchanged from <paramref name="lineNum"/> if scanning never moved).</returns>
    public (DateTime timestamp, int lineNumber) FindBackward (int lineNum, int lineCount, bool roundToSeconds, CancellationToken token = default)
    {
        lock (source.ColumnizerLock)
        {
            if (!source.Columnizer.IsTimeshiftImplemented())
            {
                return (DateTime.MinValue, lineNum);
            }

            var timestamp = DateTime.MinValue;
            var lookedBack = false;

            if (lineNum >= 0 && lineNum < lineCount)
            {
                while (timestamp.CompareTo(DateTime.MinValue) == 0 && lineNum >= 0)
                {
                    if (token.IsCancellationRequested)
                    {
                        return (DateTime.MinValue, lineNum);
                    }

                    lookedBack = true;
                    var logLine = source.Reader.GetLogLineMemory(lineNum);
                    if (logLine == null)
                    {
                        return (DateTime.MinValue, lineNum);
                    }

                    source.Callback.SetLineNum(lineNum);
                    timestamp = source.Columnizer.GetTimestamp(source.Callback, logLine);
                    if (roundToSeconds)
                    {
                        timestamp = timestamp.Subtract(TimeSpan.FromMilliseconds(timestamp.Millisecond));
                    }

                    lineNum--;
                }
            }

            if (lookedBack)
            {
                lineNum++;
            }

            return (timestamp, lineNum);
        }
    }

    /// <summary>
    /// Gets the timestamp for the line at or after <paramref name="lineNum"/>. If that line has no
    /// timestamp, the next line is checked, and so on, until one is found.
    /// </summary>
    /// <param name="lineNum">The line to start scanning forward from.</param>
    /// <param name="lineCount">The number of lines currently available (<c>ILogfileReader.LineCount</c>).</param>
    /// <param name="roundToSeconds">If true, the returned timestamp has its millisecond component zeroed.</param>
    /// <returns>The timestamp found, or <see cref="DateTime.MinValue"/> if none was, and the line
    /// number it was found on (unchanged from <paramref name="lineNum"/> if scanning never moved).</returns>
    public (DateTime timestamp, int lineNumber) FindForward (int lineNum, int lineCount, bool roundToSeconds)
    {
        lock (source.ColumnizerLock)
        {
            if (!source.Columnizer.IsTimeshiftImplemented())
            {
                return (DateTime.MinValue, lineNum);
            }

            var timestamp = DateTime.MinValue;
            var lookedForward = false;

            if (lineNum >= 0 && lineNum < lineCount)
            {
                while (timestamp.CompareTo(DateTime.MinValue) == 0 && lineNum < lineCount)
                {
                    lookedForward = true;
                    var logLine = source.Reader.GetLogLineMemory(lineNum);
                    if (logLine == null)
                    {
                        timestamp = DateTime.MinValue;
                        break;
                    }

                    source.Callback.SetLineNum(lineNum);
                    timestamp = source.Columnizer.GetTimestamp(source.Callback, logLine);
                    if (roundToSeconds)
                    {
                        timestamp = timestamp.Subtract(TimeSpan.FromMilliseconds(timestamp.Millisecond));
                    }

                    lineNum++;
                }
            }

            if (lookedForward)
            {
                lineNum--;
            }

            return (timestamp, lineNum);
        }
    }

    /// <summary>
    /// Finds the line carrying <paramref name="timestamp"/> via binary search, then walks backward
    /// to the first line of a run sharing that exact timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to search for.</param>
    /// <param name="fromLine">Line to start the binary search from.</param>
    /// <param name="lineCount">The number of lines currently available (<c>ILogfileReader.LineCount</c>).</param>
    /// <param name="roundToSeconds">If true, timestamps are compared with their millisecond component zeroed.</param>
    /// <param name="token">Checked by the underlying scans; a cancelled token unwinds the search early.</param>
    /// <returns>
    /// The line number of the first line carrying <paramref name="timestamp"/>. If no line carries
    /// it exactly, returns the line the search converged nearest to, as a normal <em>positive</em>
    /// line number — a miss degrades to "scroll here instead", it is not reported. This mirrors the
    /// original <c>FindTimestampLine</c>, whose final <c>return -foundLine</c> flipped the
    /// internal negated miss back to a scrollable line; cross-window time-sync compares timestamps
    /// at millisecond precision, so the nearest-line path is its common case, not its edge case.
    /// Callers that need the raw miss signal use <see cref="FindNearestLine"/>.
    /// </returns>
    public int FindLine (DateTime timestamp, int fromLine, int lineCount, bool roundToSeconds, CancellationToken token = default)
    {
        var foundLine = FindLineInternal(fromLine, 0, lineCount - 1, timestamp, lineCount, roundToSeconds, token);

        if (foundLine < 0)
        {
            return -foundLine;
        }

        // Walk backward to the first line of the run sharing this exact timestamp.
        var (foundTimestamp, walkedTo) = FindBackward(foundLine, lineCount, roundToSeconds, token);
        foundLine = walkedTo;
        while (foundTimestamp.CompareTo(timestamp) == 0 && foundLine >= 0)
        {
            foundLine--;
            (foundTimestamp, walkedTo) = FindBackward(foundLine, lineCount, roundToSeconds, token);
            foundLine = walkedTo;
        }

        if (foundLine < 0)
        {
            return 0;
        }

        foundLine++;
        (_, foundLine) = FindForward(foundLine, lineCount, roundToSeconds); // step to the next valid timestamp
        return foundLine;
    }

    /// <summary>
    /// The raw binary-search step, without <see cref="FindLine"/>'s walk-back to the first line of a
    /// duplicate-timestamp run. Exposed for <c>TimeSpreadCalculator</c>, which does its own
    /// (cheaper) handling of a miss and does not need the run-collapsing behaviour.
    /// </summary>
    /// <returns>The matching line, or the near-miss line negated — same convention as <see cref="FindLine"/>.</returns>
    public int FindNearestLine (DateTime timestamp, int fromLine, int rangeStart, int rangeEnd, int lineCount, bool roundToSeconds, CancellationToken token = default)
    {
        return FindLineInternal(fromLine, rangeStart, rangeEnd, timestamp, lineCount, roundToSeconds, token);
    }

    private int FindLineInternal (int lineNum, int rangeStart, int rangeEnd, DateTime timestamp, int lineCount, bool roundToSeconds, CancellationToken token)
    {
        var (currentTimestamp, foundLine) = FindBackward(lineNum, lineCount, roundToSeconds, token);
        if (currentTimestamp.CompareTo(timestamp) == 0)
        {
            return foundLine;
        }

        if (timestamp < currentTimestamp)
        {
            rangeEnd = lineNum;
        }
        else
        {
            rangeStart = lineNum;
        }

        if (rangeEnd - rangeStart <= 0)
        {
            return -lineNum;
        }

        lineNum = ((rangeEnd - rangeStart) / 2) + rangeStart;

        // Prevent an endless loop when the range can no longer be halved.
        if (rangeEnd - rangeStart < 2)
        {
            (currentTimestamp, rangeStart) = FindBackward(rangeStart, lineCount, roundToSeconds, token);
            if (currentTimestamp.CompareTo(timestamp) == 0)
            {
                return rangeStart;
            }

            (currentTimestamp, rangeEnd) = FindBackward(rangeEnd, lineCount, roundToSeconds, token);

            return currentTimestamp.CompareTo(timestamp) == 0
                ? rangeEnd
                : -lineNum;
        }

        return FindLineInternal(lineNum, rangeStart, rangeEnd, timestamp, lineCount, roundToSeconds, token);
    }
}
