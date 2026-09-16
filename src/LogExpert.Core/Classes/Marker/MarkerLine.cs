namespace LogExpert.Core.Classes.Marker;

/// <summary>One matching logical line. Lower priorities precede higher priorities in the Highlight Group.</summary>
public readonly record struct MarkerLine (int LineNumber, int ColorArgb, int Priority = 0);