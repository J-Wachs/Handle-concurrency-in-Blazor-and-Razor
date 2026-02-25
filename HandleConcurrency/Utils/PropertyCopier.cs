using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace HandleConcurrency.Utils;

/// <summary>
/// A fast property copier that works across different types.
/// Matches properties by name and compatible type.
/// Uses cached compiled delegates for high performance.
/// </summary>
public static class PropertyCopier
{
    // Cache compiled delegates for each (sourceType, targetType) combination
    private static readonly ConcurrentDictionary<(Type, Type), Action<object, object>> _cache = new();

    /// <summary>
    /// Copies properties from source to target.
    /// Properties are matched by name and type compatibility.
    /// </summary>
    /// <param name="source">The object to copy from</param>
    /// <param name="target">The object to copy to</param>
    public static void Copy(object source, object target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var key = (source.GetType(), target.GetType());

        // Get or create a compiled delegate for this type combination
        var copier = _cache.GetOrAdd(key, k => BuildCopier(k.Item1, k.Item2));

        // Execute the delegate
        copier(source, target);
    }

    /// <summary>
    /// Builds a compiled delegate that copies matching properties
    /// between sourceType and targetType.
    /// </summary>
    private static Action<object, object> BuildCopier(Type sourceType, Type targetType)
    {
        // Parameters for the delegate: (object source, object target)
        var sourceParam = Expression.Parameter(typeof(object), "source");
        var targetParam = Expression.Parameter(typeof(object), "target");

        // Convert object parameters to their actual types
        var sourceCast = Expression.Convert(sourceParam, sourceType);
        var targetCast = Expression.Convert(targetParam, targetType);

        var assignments = new List<Expression>();

        // Get all readable properties from source
        var sourceProps = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                    .Where(p => p.CanRead);

        // Get all writable properties from target, indexed by name
        var targetProps = targetType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                                    .Where(p => p.CanWrite)
                                    .ToDictionary(p => p.Name);

        // Match properties by name and compatible type
        foreach (var sProp in sourceProps)
        {
            if (targetProps.TryGetValue(sProp.Name, out var tProp)
                && tProp.PropertyType.IsAssignableFrom(sProp.PropertyType))
            {
                // Generate assignment: target.Prop = source.Prop
                var assign = Expression.Assign(
                    Expression.Property(targetCast, tProp),
                    Expression.Property(sourceCast, sProp)
                );
                assignments.Add(assign);
            }
        }

        // Combine all assignments into a single block expression
        var body = Expression.Block(assignments);

        // Compile the expression into a delegate
        var lambda = Expression.Lambda<Action<object, object>>(body, sourceParam, targetParam);
        return lambda.Compile();
    }
}
