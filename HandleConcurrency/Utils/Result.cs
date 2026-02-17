using Microsoft.AspNetCore.Http;
using System.Net;
using System.Text.Json.Serialization;

namespace HandleConcurrency.Utils;

///
/// Result Pattern.
///

/// <summary>
/// Represents a single message, which can optionally be associated with a specific data field.
/// </summary>
/// <param name="FieldName">The name of the field the message relates to. Can be null for general messages.</param>
/// <param name="Message">The content of the message.</param>
public record ResultMessage(string? FieldName, string Message)
{
};

/// <summary>
/// Represents a collection of messages grouped under a single field name.
/// </summary>
public class ResultMessages
{
    /// <summary>
    /// The name of the field these messages apply to.
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// The list of message strings for the specified field.
    /// </summary>
    public List<string> Messages { get; set; } = [];
}

public enum ResultCode
{
    Unknown = AbstractResult.Zero,
    Ok = StatusCodes.Status200OK,
    Created = StatusCodes.Status201Created,
    BadRequest = StatusCodes.Status400BadRequest,
    Unauthorized = StatusCodes.Status401Unauthorized,
    Forbidden = StatusCodes.Status403Forbidden,
    NotFound = StatusCodes.Status404NotFound,
    Conflict = StatusCodes.Status409Conflict, 
    ServerError = StatusCodes.Status500InternalServerError
}

/// <summary>
/// An abstract base class for operation results, encapsulating success status and a list of messages.
/// </summary>
public abstract class AbstractResult
{
    public const int Zero = 0;

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; private set; }

    /// <summary>
    /// Gets the list of messages associated with the result, grouped by field name.
    /// </summary>
    public List<ResultMessages> Messages { get; private set; } = [];

    /// <summary>
    /// Result code of the result object.
    /// </summary>
    public ResultCode ResultCode { get; private set; }

    /// <summary>
    /// Protected constructor for derived result classes.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">An optional list of messages to associate with the result</param>
    protected AbstractResult(ResultCode resultCode, List<ResultMessages> messages)
    {
        IsSuccess = resultCode is ResultCode.Ok or ResultCode.Created;

        ResultCode = resultCode;

        if (messages is not null && messages.Count is not 0)
        {
            AddListOfResultMessages(messages);
        }
    }

    /// <summary>
    /// Adds a list of ResultMessages, grouping them by field name.
    /// </summary>
    /// <param name="messages">The list of messages to add.</param>
    private void AddListOfResultMessages(List<ResultMessages> messages)
    {
        foreach (var oneFieldMessage in messages)
        {
            var fieldMessageEntry = Messages.Find(x => x.FieldName == oneFieldMessage.FieldName);
            if (fieldMessageEntry is not null)
            {
                // If messages for this field already exist, add the new ones.
                fieldMessageEntry.Messages.AddRange(oneFieldMessage.Messages);
            }
            else
            {
                // Otherwise, create a new entry for this field.
                Messages.Add(new()
                {
                    FieldName = oneFieldMessage.FieldName,
                    Messages = [.. oneFieldMessage.Messages]
                });
            }
        }
    }
}

/// <summary>
/// Represents the result of an operation that does not return a value.
/// </summary>
/// <param name="resultCode">The success status of the operation.</param>
/// <param name="messages">The list of messages associated with the result.</param>
public class Result : AbstractResult
{
    /// <summary>
    /// Private constructor to create a new generic result instance. Use static factory methods instead.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">The list of messages associated with the result.</param>
    [JsonConstructor]
    private Result(ResultCode resultCode, List<ResultMessages> messages) : base(resultCode, messages)
    {
    }

    /// <summary>
    /// Creates a successful result with no messages.
    /// </summary>
    /// <returns>A new successful Result instance.</returns>
    public static Result Success() => new(ResultCode.Ok, []);

    /// <summary>
    /// Creates a successful result with a single, general message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new successful Result instance with a message.</returns>
    public static Result Success(string message) => new(ResultCode.Ok, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a successful result with a provided list of messages.
    /// </summary>
    /// <param name="messages">The list of success messages.</param>
    /// <returns>A new successful Result instance with messages.</returns>
    public static Result Success(List<ResultMessages> messages) => new(ResultCode.Ok, messages);

    /// <summary>
    /// Creates a failed result with a single, general message.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with a message.</returns>
    public static Result Failure(string message) => new(ResultCode.BadRequest, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a failed result with a single message associated with a specific field.
    /// </summary>
    /// <param name="message">The result message containing field and message text.</param>
    /// <returns>A new failed Result instance with a message.</returns>
    public static Result Failure(ResultMessage message) => new(ResultCode.BadRequest, [new() { FieldName = message.FieldName, Messages = [message.Message] }]);

    /// <summary>
    /// Creates a failed bad request result with a single message associated with a specific field.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with a message.</returns>
    public static Result FailureBadRequest(string message) => new(ResultCode.BadRequest, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a failed conflict result with a single message associated with a specific field.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with a message.</returns>
    public static Result FailureConflict(string message) => new(ResultCode.Conflict, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a failed not found result with a provided list of messages.
    /// </summary>
    /// <param name="message">The success message.</param>
    /// <returns>A new failed Result instance with messages.</returns>
    public static Result FailureNotFound(string message) => new(ResultCode.NotFound, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a fatal result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result Fatal(string message) => new(ResultCode.ServerError, [new() { FieldName = default, Messages = [message] }]);

    /// <summary>
    /// Creates a new Result instance by copying the state of another non-generic Result.
    /// </summary>
    /// <param name="result">The result to copy.</param>
    /// <returns>A new Result instance with the same properties.</returns>
    public static Result CopyResult(Result result) => new(result.ResultCode, result.Messages);

    /// <summary>
    /// Creates a non-generic Result by copying the state from a generic Result, discarding its data.
    /// </summary>
    /// <typeparam name="TResult">The data type of the source result.</typeparam>
    /// <param name="result">The generic result to copy from.</param>
    /// <returns>A new non-generic Result instance.</returns>
    public static Result CopyResult<TResult>(Result<TResult> result) => new(result.ResultCode, result.Messages);

    /// <summary>
    /// TODO: Skriv dokumentation.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Result MapFromStatusCode(HttpStatusCode statusCode, string message)
    {
        if (Enum.IsDefined(typeof(ResultCode), statusCode))
        {
            ResultCode resultCode = (ResultCode)statusCode;
            return new Result(resultCode, [new() { FieldName = default, Messages = [message] }]);
        }
        else
        {
            return Result.FailureBadRequest(message);
        }
    }
}

/// <summary>
/// Represents the result of an operation that returns a value of type T.
/// </summary>
/// <typeparam name="T">The type of the data returned by the operation.</typeparam>
public class Result<T> : AbstractResult
{
    /// <summary>
    /// The data payload of the result. It will be default (e.g., null) if the operation failed.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Private constructor to create a new generic result instance. Use static factory methods instead.
    /// </summary>
    /// <param name="resultCode">The result code for the success/error state.</param>
    /// <param name="messages">The list of messages associated with the result.</param>
    /// <param name="data">The data payload of the result.</param>
    [JsonConstructor]
    private Result(ResultCode resultCode, List<ResultMessages> messages, T? data) : base(resultCode, messages)
    {
        Data = data;
    }

    /// <summary>
    /// Creates a successful result with a data payload.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <returns>A new successful Result instance with data.</returns>
    public static Result<T> Success(T data) => new(ResultCode.Ok, [], data);

    /// <summary>
    /// Creates a successful result with a data payload and a single message.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <param name="message">The result message.</param>
    /// <returns>A new successful Result instance with data and a message.</returns>
    public static Result<T> Success(T data, ResultMessage message) => new(ResultCode.Ok, [new() { FieldName = message.FieldName, Messages = [message.Message] }], data);

    /// <summary>
    /// Creates a successful result with a data payload and a single, general message.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A new successful Result instance with data and a message.</returns>
    public static Result<T> Success(T data, string message) => new(ResultCode.Ok, [new() { FieldName = default, Messages = [message] }], data);

    /// <summary>
    /// Creates a successful result with a data payload and a list of messages.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <param name="messages">The list of messages.</param>
    /// <returns>A new successful Result instance with data and messages.</returns>
    public static Result<T> Success(T data, List<ResultMessages> messages) => new(ResultCode.Ok, messages, data);

    /// <summary>
    /// Creates a successful created result with a data payload.
    /// </summary>
    /// <param name="data">The data payload.</param>
    /// <returns>A new successful Result instance with data.</returns>
    public static Result<T> SuccessCreated(T data) => new(ResultCode.Created, [], data);

    /// <summary>
    /// Creates a failed result with a single message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The result message containing field and message text.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Failure(ResultMessage message) => new(ResultCode.BadRequest, [new() { FieldName = message.FieldName, Messages = [message.Message] }], default);

    /// <summary>
    /// Creates a failed result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Failure(string message) => new(ResultCode.BadRequest, [new() { FieldName = default, Messages = [message] }], default);

    /// <summary>
    /// Creates a failed result with a single message associated with a specific field. Data will be the default for type T.
    /// </summary>
    /// <param name="fieldName">The name of the field the message relates to.</param>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Failure(string fieldName, string message) => new(ResultCode.BadRequest, [new() { FieldName = fieldName, Messages = [message] }], default);

    /// <summary>
    /// Creates a failed result with a list of messages. Data will be the default for type T.
    /// </summary>
    /// <param name="messages">The list of failure messages.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Failure(List<ResultMessages> messages) => new(ResultCode.BadRequest, messages, default);

    /// <summary>
    /// Creates a failed result that includes the original data (e.g., the invalid entity) and a message.
    /// </summary>
    /// <param name="data">The data payload, typically the object that failed validation.</param>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance with data and a message.</returns>
    public static Result<T> Failure(T data, string message) => new(ResultCode.BadRequest, [new() { FieldName = default, Messages = [message] }], data);

    /// <summary>
    /// Creates a failed result that includes the original data and a list of messages.
    /// </summary>
    /// <param name="data">The data payload, typically the object that failed validation.</param>
    /// <param name="messages">The list of failure messages.</param>
    /// <returns>A new failed Result instance with data and messages.</returns>
    public static Result<T> Failure(T data, List<ResultMessages> messages) => new(ResultCode.BadRequest, messages, data);

    /// <summary>
    /// Creates a failed bad request result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureBadRequest(string message) => new(ResultCode.BadRequest, [new() { FieldName = default, Messages = [message] }], default);

    /// <summary>
    /// Creates a failed conflict result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureConflict(string message) => new(ResultCode.NotFound, [new() { FieldName = default, Messages = [message] }], default);

    /// <summary>
    /// Creates a failed result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> FailureNotFound(string message) => new(ResultCode.NotFound, [new() { FieldName = default, Messages = [message] }], default);

    /// <summary>
    /// Creates a fatal result with a single, general message. Data will be the default for type T.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <returns>A new failed Result instance.</returns>
    public static Result<T> Fatal(string message) => new(ResultCode.ServerError, [new() { FieldName = default, Messages = [message] }], default);

    /// <summary>
    /// Creates a new generic Result by copying the state from a non-generic Result. Data will be the default for type T.
    /// </summary>
    /// <param name="result">The non-generic result to copy.</param>
    /// <returns>A new generic Result instance.</returns>
    public static Result<T> CopyResult(Result result) => new(result.ResultCode, result.Messages, default);

    /// <summary>
    /// Creates a new generic Result by copying the state of another generic Result.
    /// </summary>
    /// <param name="result">The generic result to copy.</param>
    /// <returns>A new generic Result instance with the same properties.</returns>
    public static Result<T> CopyResult<T2>(Result<T2> result) => new(result.ResultCode, result.Messages, default);

    /// <summary>
    /// TODO: Skriv dokumentation.
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    public static Result<T> MapFromStatusCode(HttpStatusCode statusCode, string message)
    {
        if (Enum.IsDefined(typeof(ResultCode), (int)statusCode))
        {
            ResultCode resultCode = (ResultCode)statusCode;
            return new Result<T>(resultCode, [new() { FieldName = default, Messages = [message] }], default);
        }
        else
        {
            return Result<T>.FailureBadRequest(message);
        }
    }
}
