using HandleConcurrencyBlazorDemo.Utils.Interfaces;
using System.Linq.Expressions;
using System.Text;

namespace HandleConcurrencyBlazorDemo.Utils;

/// <summary>
/// This visitor traverses an Expression Tree and builds a URL query string. 
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <param name="logger"></param>
public class UrlQueryVisitor<TEntity>(ILogger<UrlQueryVisitor<TEntity>> logger) : ExpressionVisitor, IUrlQueryVisitor<TEntity> where TEntity : class
{
    private readonly StringBuilder _queryBuilder = new();

    public Result<string?> BuildFromExpression(Expression<Func<TEntity, bool>>? filter)
    {
        try
        {
            if (filter is null)
            {
                return Result<string?>.Success(string.Empty);
            }

            // We only need the body of the lambda expression.
            return Result<string?>.Success(ToQueryString(filter.Body));
        }
        catch (Exception ex)
        {
            string methodName = $"{nameof(BuildFromExpression)}", paramList = "(filter)";
            logger.LogCritical("An error occured in '{methodName}{paramList}'. The error is '{ex}'.", methodName, paramList, ex);
            return Result<string?>.Failure($"An error occured in '{methodName}'.The error is '{ex.Message}'.");
        }
    }

    /// <summary>
    /// Entry point to start the conversion. 
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    protected string ToQueryString(Expression expression)
    {
        _queryBuilder.Clear();
        Visit(expression);
        return _queryBuilder.ToString();
    }

    /// <summary>
    /// This method is called for binary operations like ==, >=, &&, etc. 
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    protected override Expression VisitBinary(BinaryExpression node)
    {
        // We only support 'AND' combinations. 'OR' cannot be represented
        // in the simple key[op]=value format.
        if (node.NodeType == ExpressionType.OrElse)
        {
            throw new NotSupportedException("OR expressions are not supported in the query string builder.");
        }

        // If it's an 'AND', we visit both sides to process them.
        if (node.NodeType == ExpressionType.AndAlso)
        {
            Visit(node.Left);
            Visit(node.Right);
            return node;
        }

        // Now, handle comparison operations (e.g., property == constant).
        MemberExpression memberExpr;
        ConstantExpression constantExpr;

        // Handles both "property == constant" and "constant == property"
        if (node.Left is MemberExpression m && node.Right is ConstantExpression c)
        {
            memberExpr = m;
            constantExpr = c;
        }
        else if (node.Right is MemberExpression m2 && node.Left is ConstantExpression c2)
        {
            memberExpr = m2;
            constantExpr = c2;
        }
        else
        {
            // The expression is too complex for us to parse.
            return base.VisitBinary(node);
        }

        string propertyName = memberExpr.Member.Name;
        string op = OperatorToString(node.NodeType);
        string value = Uri.EscapeDataString(constantExpr.Value?.ToString() ?? string.Empty);

        AppendQueryPart($"{propertyName}[{op}]={value}");

        return node;
    }

    /// <summary>
    /// This method is called for method calls like .Contains(), .StartsWith(), etc.
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // Ensure it's a method call on a property (e.g., x.Name.Contains(...))
        if (node.Object is not MemberExpression memberExpr)
        {
            return base.VisitMethodCall(node);
        }

        // Ensure the argument is a simple constant value.
        if (node.Arguments.FirstOrDefault() is not ConstantExpression constantExpr)
        {
            return base.VisitMethodCall(node);
        }

        string propertyName = memberExpr.Member.Name;
        string op = OperatorToString(node.Method.Name);
        string value = Uri.EscapeDataString(constantExpr.Value?.ToString() ?? "");

        if (!string.IsNullOrEmpty(op))
        {
            AppendQueryPart($"{propertyName}[{op}]={value}");
        }

        return node;
    }

    /// <summary>
    /// Build query string.
    /// </summary>
    /// <param name="part"></param>
    private void AppendQueryPart(string part)
    {
        if (_queryBuilder.Length > 0)
        {
            _queryBuilder.Append('&');
        }
        _queryBuilder.Append(part);
    }

    /// <summary>
    /// Helper to map ExpressionType to our URL operator strings. 
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static string OperatorToString(ExpressionType type) => type switch
    {
        ExpressionType.Equal => "eq",
        ExpressionType.NotEqual => "ne",
        ExpressionType.GreaterThan => "gt",
        ExpressionType.GreaterThanOrEqual => "gte",
        ExpressionType.LessThan => "lt",
        ExpressionType.LessThanOrEqual => "lte",
        _ => ""
    };

    /// <summary>
    /// Helper to map method names to our URL operator strings. 
    /// </summary>
    /// <param name="methodName"></param>
    /// <returns></returns>
    private static string OperatorToString(string methodName) => methodName.ToLowerInvariant() switch
    {
        "contains" => "like",
        "startswith" => "startswith",
        "endswith" => "endswith",
        _ => ""
    };
}
