using HandleConcurrency.Data;
using HandleConcurrency.Helpers;
using System.Linq.Expressions;

namespace HandleConcurrency.Utils;

public static class QueryableExtensions
{
    // This method takes a list of our sort descriptor objects
    // and applies them to an IQueryable by building expressions manually.
    public static IQueryable<TEntity> ApplySorting<TEntity>(this IQueryable<TEntity> query, IEnumerable<SortDescriptor> sorting) where TEntity : AbstractBaseTable
    {
        if (sorting is null || !sorting.Any())
        {
            return query.OrderBy(e => e.Id);
        }

        bool isFirstField = true;

        foreach (var descriptor in sorting)
        {
            // Create a parameter for the lambda expression (e.g., "x =>")
            var parameter = Expression.Parameter(typeof(TEntity), "x");

            // Create access to the property (e.g., "x.Navn")
            // Important: In a real application, you should check that the property exists
            // to avoid runtime exceptions if an invalid field name is provided.
            var propertyAccess = Expression.Property(parameter, descriptor.FieldName);

            // Create the full lambda expression (e.g., "x => x.Navn")
            var lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName;
            if (isFirstField)
            {
                methodName = descriptor.Direction == SortDirection.Descending ? "OrderByDescending" : "OrderBy";
                isFirstField = false;
            }
            else
            {
                methodName = descriptor.Direction == SortDirection.Descending ? "ThenByDescending" : "ThenBy";
            }

            // Build the final method call dynamically
            query = (IQueryable<TEntity>)query.Provider.CreateQuery(
                Expression.Call(
                    typeof(Queryable),
                    methodName,
                    [typeof(TEntity), propertyAccess.Type],   // Generic type arguments
                    query.Expression,                         // The existing query expression
                    Expression.Quote(lambda)                  // The lambda for the property to sort by
                )
            );
        }

        return query;
    }
}
