namespace HandleConcurrency.Models;

/// <summary>
/// Represents the details of a unique constraint violation in a database operation.
/// </summary>
/// <remarks>This record provides information about the specific field, index, key value, and table involved in a
/// unique constraint violation. It is typically used to aid in error handling and debugging scenarios where database
/// uniqueness constraints are enforced.</remarks>
public record UniqueConstraintViolationDetails
{
    public string FieldName { get; init; } = string.Empty;
    public string IndexName { get; init; } = string.Empty;
    public string KeyValue { get; init; } = string.Empty;
    public string TableName { get; init; } = string.Empty;
}
