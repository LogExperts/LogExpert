namespace LogExpert.Core.Classes.Marker;

/// <summary>One matching logical line. Lower priorities precede higher priorities in the Highlight Group.</summary>
/// <param name="ColorArgb">The rule color, or null to inherit the foreground supplied during aggregation.</param>
public readonly record struct MarkerLine (int LineNumber, int? ColorArgb, int Priority = 0);