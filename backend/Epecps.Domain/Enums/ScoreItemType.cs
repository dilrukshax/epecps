namespace Epecps.Domain.Enums;

/// <summary>
/// Defines the type of scoring mechanism for a score item
/// </summary>
public enum ScoreItemType
{
    /// <summary>
    /// Checklist item (completed/not completed)
    /// </summary>
    Checklist = 0,

    /// <summary>
    /// Rating scale (e.g., 1-5 stars)
    /// </summary>
    Rating = 1,

    /// <summary>
    /// Numeric value entry
    /// </summary>
    Numeric = 2
}
