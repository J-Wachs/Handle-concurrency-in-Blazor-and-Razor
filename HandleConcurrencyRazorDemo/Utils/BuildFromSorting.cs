using HandleConcurrency.Helpers;

namespace HandleConcurrencyRazorDemo.Utils;

/// <summary>
/// Build the sorting of data.
/// </summary>
public static class BuildFromSorting
{
    /// <summary>
    /// Builds a querystring for the sorting of data
    /// </summary>
    /// <param name="sorting"></param>
    /// <returns>Query string in the format sort={-}field_name</returns>
    public static string Build(IEnumerable<SortDescriptor> sorting)
    {
        // If there is no sorting, return an empty string.
        if (sorting is null || !sorting.Any())
        {
            return string.Empty;
        }

        // Use LINQ to transform each SortDescriptor into its string representation.
        // - Descending gets a minus prefix.
        // - Ascending gets no prefix.
        var sortParts = sorting.Select(d =>
            d.Direction == SortDirection.Descending
                ? $"-{d.FieldName}"
                : d.FieldName
        );

        // Join all the parts with a comma and add the "sort=" prefix.
        return $"sort={string.Join(",", sortParts)}";
    }
}
