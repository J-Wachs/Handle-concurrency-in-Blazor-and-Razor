using Microsoft.AspNetCore.Http;

namespace HandleConcurrency.Utils;

/// <summary>
/// Extension that will return a http response based on the result object's 
/// result code.
/// </summary>
public static class ResultAsIResult
{
    /// <summary>
    /// Return the result object as response with the http status from
    /// the result object's result code.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static IResult AsIResult(this Result result)
    {
        return Results.Json(result, statusCode: (int)result.ResultCode);
    }

    /// <summary>
    /// Return the result object as response with the http status from
    /// the result object's result code.
    /// </summary>
    /// <typeparam name="T">Type of the data contained in the result object.</typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static IResult AsIResult<T>(this Result<T> result)
    {
        return Results.Json(result, statusCode: (int)result.ResultCode);
    }
}
