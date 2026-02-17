using System.Linq.Expressions;

namespace HandleConcurrencyBlazorDemo.Utils.Interfaces;

public interface IUrlQueryVisitor<TEntity> where TEntity : class
{
    /// <summary>
    /// Builds a query string from the filter (conditions for data).
    /// </summary>
    /// <param name="filter">The filter to build query string for</param>
    /// <returns>Query string in syntac for selection of data</returns>
    Result<string?> BuildFromExpression(Expression<Func<TEntity, bool>>? filter);
}
