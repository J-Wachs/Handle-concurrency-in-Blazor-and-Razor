using HandleConcurrency.Helpers;
using HandleConcurrency.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HandleConcurrency.Utils;

/// <summary>
/// Parses error messages from the SQL engine and return selected information from the error messages.
/// </summary>
public static partial class SqlExceptionParser
{
    // The Regex uses named capture groups (?<name>...) to make the code more readable.

    /// <summary>
    /// The english version of the duplicate key row in index. Change it if your version returns the message in another language.
    /// </summary>
    private const string UniqueConstraintPattern =
        @"Cannot insert duplicate key row in object '(?<TableName>\w+\.\w+)' with unique index '(?<IndexName>\w+)'. The duplicate key value is \((?<KeyValue>.+)\).";

    [GeneratedRegex(UniqueConstraintPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UniqueConstraintRegex();

    /// <summary>
    /// Tries to parse a DbUpdateException to finde information about a unique constraint violation.
    /// </summary>
    /// <returns>REsult object containing a information object, if succes in parsing the message</returns>
    public static Result<UniqueConstraintViolationDetails?> TryGetUniqueConstraintViolationDetails(this DbUpdateException dbUpdateException)
    {
        // Find the inner SqlException
        if (dbUpdateException.GetBaseException() is not SqlException sqlException)
        {
            return Result<UniqueConstraintViolationDetails?>.Failure("Exception is not of type SqlException.");
        }

        // Check if it is the correct error type. You could extend this for other error types.
        if (sqlException.Number is not (int)SqlErrors.ViolationUniqueKeyConstraint)
        {
            return Result<UniqueConstraintViolationDetails?>.Failure($"The error is not a '{nameof(SqlErrors.ViolationUniqueKeyConstraint)}'.");
        }

        var match = UniqueConstraintRegex().Match(sqlException.Message);
        if (match.Success is false)
        {
            return Result<UniqueConstraintViolationDetails?>.Failure($"The error message does not match a '{nameof(SqlErrors.ViolationUniqueKeyConstraint)}' error message.");
        }

        // Filter out the actual table name, dropping the owner:
        var tableName = match.Groups["TableName"].Value;
        if (tableName.Contains('.'))
        {
            // I do not want the user to see the table name as 'dbo.tablename', it must
            // be as 'tablenale'. So Split() somes to the rescue:
            tableName = tableName.Split('.').Last();
        }

        // Try to get the FieldName. The prerequisite is that index name follows the structure
        // 'IX_tablename_fieldname':
        var indexName = match.Groups["IndexName"].Value;
        var fieldName = "(Sorry, cannot resolve it)";
        if (indexName.Contains('_'))
        {
            fieldName = indexName.Split('_').Last();
        }

        return Result<UniqueConstraintViolationDetails?>.Success(new UniqueConstraintViolationDetails
                                                                    {
                                                                        FieldName = fieldName,
                                                                        IndexName = match.Groups["IndexName"].Value,
                                                                        KeyValue = match.Groups["KeyValue"].Value,
                                                                        TableName = tableName
                                                                    });
    }
}
