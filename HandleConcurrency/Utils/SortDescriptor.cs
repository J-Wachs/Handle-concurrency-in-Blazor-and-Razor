using HandleConcurrency.Helpers;

namespace HandleConcurrency.Utils;

/// <summary>
/// Information about how data is to be sorted.
/// </summary>
public class SortDescriptor
{
    /// <summary>
    /// The name of the field to sort data by.
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// The direction to sort the data in field 'FieldName'.
    /// </summary>
    public SortDirection Direction { get; set; }
}
