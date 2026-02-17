using Microsoft.AspNetCore.Components.QuickGrid;

namespace HandleConcurrencyBlazorDemo.Utils;

public static class QuickGridSortConverter
{
    /// <summary>
    /// Converts a collection of QuickGrid's SortedProperty objects
    /// into a list of our own SortDescriptor objects.
    /// </summary>
    /// <param name="sortByProperties">The collection of sort properties from the GridItemsProviderRequest.</param>
    /// <returns>A list of SortDescriptor objects, ready to be used in API calls.</returns>
    public static IEnumerable<SortDescriptor> ToSortDescriptors(IReadOnlyCollection<SortedProperty> sortByProperties)
    {
        // If the input is null, return an empty list to avoid errors.
        if (sortByProperties is null)
        {
            return Enumerable.Empty<SortDescriptor>();
        }

        // Use LINQ's .Select to transform each element from one type to the other.
        return sortByProperties.Select(prop => new SortDescriptor
        {
            // The property name can be transferred directly.
            FieldName = prop.PropertyName,

            // We need to translate from QuickGrid's SortDirection enum to our own.
            // This is easily done with a ternary operator.
            Direction = prop.Direction == Microsoft.AspNetCore.Components.QuickGrid.SortDirection.Ascending
                ? HandleConcurrency.Helpers.SortDirection.Ascending
                : HandleConcurrency.Helpers.SortDirection.Descending
        });
    }
}
