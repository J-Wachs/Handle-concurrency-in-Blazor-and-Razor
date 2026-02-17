using HandleConcurrency.Helpers;
using HandleConcurrencyBlazorDemo.Controllers.Utils.Interfaces;
using System.Linq.Expressions;
using System.Text.RegularExpressions;

namespace HandleConcurrencyBlazorDemo.Controllers.Utils;

/// <summary>
/// Parsing filter and sorting to build filter expression and sorting.
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
public class APIQueryParser<TEntity> : IAPIQueryParser<TEntity> where TEntity : class
{
    private List<string> AllowedFields = [];

    /// <summary>
    /// Set the list of allowed fields. If fields in sort and filter is not in this list, then
    /// failure is reported.
    /// </summary>
    /// <param name="allowedFields"></param>
    public void SetAllowedFields(List<string> allowedFields)
    {
        AllowedFields = allowedFields;
    }

    /// <summary>
    /// Parses a set of filter tags from the query string and converts when to an expression.
    /// </summary>
    /// <param name="queryParams">part of the query string that contains the filter rules</param>
    /// <returns>Expression to use for selection of data</returns>
    public Result<Expression<Func<TEntity, bool>>?> ParseFilter(IQueryCollection queryParams)
    {
        Expression<Func<TEntity, bool>> fullPredicate = default!;

        foreach (var param in queryParams)
        {
            var match = RegExEngine.ContainsSquareBrackets().Match(param.Key);
            if (!match.Success) continue;

            var propertyName = match.Groups[1].Value;

            // Check against the dynamically discovered allowed fields for type T.
            if (!AllowedFields.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
            {
                // continue;
                return Result<Expression<Func<TEntity, bool>>?>.Failure($"The field '{propertyName}' is not allowed to use.");
            }

            var op = match.Groups[2].Value.ToLowerInvariant();
            var value = param.Value.ToString();

            // The parameter type is now generic.
            var parameter = Expression.Parameter(typeof(TEntity), "x");
            var property = Expression.Property(parameter, propertyName);

            Expression? predicate = default;

            // ***************************************************************
            // EXPANDED SWITCH STATEMENT STARTS HERE
            // ***************************************************************

            switch (op)
            {
                // -- EQUALITY OPERATORS --

                case "eq": // Equal: property == value
                    predicate = Expression.Equal(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                case "ne": // Not Equal: property != value
                    predicate = Expression.NotEqual(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                // -- COMPARISON OPERATORS --

                case "gt": // Greater Than: property > value
                    predicate = Expression.GreaterThan(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                case "gte": // Greater Than or Equal: property >= value
                    predicate = Expression.GreaterThanOrEqual(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                case "lt": // Less Than: property < value
                    predicate = Expression.LessThan(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                case "lte": // Less Than or Equal: property <= value
                    predicate = Expression.LessThanOrEqual(property, Expression.Constant(Convert.ChangeType(value, property.Type)));
                    break;

                // -- STRING OPERATORS --
                // These should only be used on string properties.

                case "like":
                case "contains": // String Contains: property.Contains(value)
                    if (property.Type != typeof(string)) continue;
                    var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
                    if (containsMethod is not null)
                    {
                        predicate = Expression.Call(property, containsMethod, Expression.Constant(value, typeof(string)));
                    }
                    break;

                case "notlike":
                case "doesnotcontain": // String Does Not Contain: !property.Contains(value)
                    if (property.Type != typeof(string)) continue;
                    var notContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)]);
                    if (notContainsMethod is not null)
                    {
                        predicate = Expression.Not(Expression.Call(property, notContainsMethod, Expression.Constant(value, typeof(string))));
                    }
                    break;

                case "startswith": // String Starts With: property.StartsWith(value)
                    if (property.Type != typeof(string)) continue;
                    var startsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)]);
                    if (startsWithMethod is not null)
                    {
                        predicate = Expression.Call(property, startsWithMethod, Expression.Constant(value, typeof(string)));
                    }
                    break;

                case "endswith": // String Ends With: property.EndsWith(value)
                    if (property.Type != typeof(string)) continue;
                    var endsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)]);
                    if (endsWithMethod is not null)
                    {
                        predicate = Expression.Call(property, endsWithMethod, Expression.Constant(value, typeof(string)));
                    }
                    break;

                // -- LIST OPERATORS --

                case "in": // Is in list: new[] { "a", "b" }.Contains(property)
                    var values = value.Split(',').Select(v => Convert.ChangeType(v.Trim(), property.Type)).ToList();
                    var listType = typeof(List<>).MakeGenericType(property.Type);
                    var listContainsMethod = listType.GetMethod("Contains", [property.Type]);
                    var listConstant = Expression.Constant(values, listType);
                    if (listContainsMethod is not null)
                    {
                        predicate = Expression.Call(listConstant, listContainsMethod, property);
                    }
                    break;

                case "notin": // Is not in list: !new[] { "a", "b" }.Contains(property)
                    var notInValues = value.Split(',').Select(v => Convert.ChangeType(v.Trim(), property.Type)).ToList();
                    var notInListType = typeof(List<>).MakeGenericType(property.Type);
                    var notInListContainsMethod = notInListType.GetMethod("Contains", [property.Type]);
                    var notInListConstant = Expression.Constant(notInValues, notInListType);
                    if (notInListContainsMethod is not null)
                    {
                        predicate = Expression.Not(Expression.Call(notInListConstant, notInListContainsMethod, property));
                    }
                    break;

                default:
                    // If the operator is unknown, return failure.
                    return Result<Expression<Func<TEntity, bool>>?>.Failure($"The operator '{op}' is not allowed.");
            }

            if (predicate is not null)
            {
                // The lambda type is also generic.
                var lambda = Expression.Lambda<Func<TEntity, bool>>(predicate, parameter);

                // Combine with previous predicates using "AND" logic.
                fullPredicate = fullPredicate == null
                    ? lambda
                    : Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(fullPredicate.Body, lambda.Body), fullPredicate.Parameters);
            }
        }

        return Result<Expression<Func<TEntity, bool>>?>.Success(fullPredicate);
    }

    /// <summary>
    /// Parses the sting  for the sorting syntax and creates a list of SortDescriptors.
    /// </summary>
    /// <param name="sortParam"></param>
    /// <returns></returns>
    public Result<IEnumerable<SortDescriptor>?> ParseSort(string? sortParam)
    {
        if (string.IsNullOrWhiteSpace(sortParam))
        {
            return Result<IEnumerable<SortDescriptor>?>.Success([]);
        }

        var descriptors = new List<SortDescriptor>();
        var fields = sortParam.Split(',');

        foreach (var field in fields)
        {
            var isDescending = field.StartsWith('-');
            var propertyName = isDescending ? field.TrimStart('-') : field.Trim();

            if (!AllowedFields.Contains(propertyName, StringComparer.OrdinalIgnoreCase))
            {
                return Result<IEnumerable<SortDescriptor>?>.Failure($"The field '{propertyName}' is not allowed to use.");
            }

            descriptors.Add(new SortDescriptor
            {
                FieldName = propertyName,
                Direction = isDescending ? SortDirection.Descending : SortDirection.Ascending
            });
        }

        return Result<IEnumerable<SortDescriptor>?>.Success(descriptors);
    }
}

/// <summary>
/// REgular expresion.
/// </summary>
internal static partial class RegExEngine
{
    private const string _pattern = @"(\w+)\[(\w+)\]";
    [GeneratedRegex(_pattern, RegexOptions.IgnoreCase)]
    internal static partial Regex ContainsSquareBrackets();
}
