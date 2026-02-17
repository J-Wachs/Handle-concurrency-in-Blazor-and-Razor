using System.Linq.Expressions;

namespace HandleConcurrencyBlazorDemo.Controllers.Utils.Interfaces;

/// <summary>
/// Interface for parsing filter and sorting to build filter expression and sorting.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public interface IAPIQueryParser<TEntity> where TEntity : class
{
    /// <summary>
    /// Parse filter and build filter expression.
    /// </summary>
    /// <param name="queryParams"></param>
    /// <returns></returns>
    Result<Expression<Func<TEntity, bool>>?> ParseFilter(IQueryCollection queryParams);

    /// <summary>
    /// Build list of sort descripters for sorting.
    /// </summary>
    /// <param name="sortParam"></param>
    /// <returns></returns>
    Result<IEnumerable<SortDescriptor>?> ParseSort(string? sortParam);

    /// <summary>
    /// Sets the allowed fields in filter and sorting.
    /// </summary>
    /// <param name="allowedFields"></param>
    void SetAllowedFields(List<string> allowedFields);
}
