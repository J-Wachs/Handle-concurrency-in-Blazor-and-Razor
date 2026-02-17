namespace HandleConcurrency.Utils;

/// <summary>
/// Extension method for Result class. Format messages into one string.
/// </summary>
public static class ResultFormatMessage
{
    /// <summary>
    /// Returnes the embedded message object as a formatted string
    /// with fields names and messages.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public static string FormatMessages(this Result result)
    {
        return FormatTheMessages(result.Messages);
    }

    /// <summary>
    /// Returnes the embedded message object as a formatted string
    /// with fields names and messages.
    /// </summary>
    /// <typeparam name="T">Type of the data contained in the result object.</typeparam>
    /// <param name="result"></param>
    /// <returns></returns>
    public static string FormatMessages<T>(this Result<T> result)
    {
        return FormatTheMessages(result.Messages);
    }

    /// <summary>
    /// Concatenates the field name with it's messages and return one string.
    /// </summary>
    /// <param name="messages"></param>
    /// <returns></returns>
    private static string FormatTheMessages(List<ResultMessages> messages)
    {
        string message = string.Empty;

        foreach (var oneField in messages)
        {
            if (!string.IsNullOrEmpty(message))
            {
                message += ", ";
            }
            if (!string.IsNullOrEmpty(oneField.FieldName))
            {
                message += oneField.FieldName + ": ";
            }

            message += String.Join(", ", oneField.Messages);
        }

        return message;
    }
}
