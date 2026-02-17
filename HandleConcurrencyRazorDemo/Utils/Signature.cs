using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace HandleConcurrencyRazorDemo.Utils;

/// <summary>
/// Builds a signature form a list of fields/values
/// </summary>
public static class Signature
{
    /// <summary>
    /// Calculates a SHA256 signature of the values in the list 'values' 
    /// </summary>
    /// <param name="fields"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Calculate(params object[] values)
    {
        if (values is null || values.Length is 0)
            throw new ArgumentException("At least one value must be provided", nameof(values));

        // Convert each field to string using invariant culture and handle null values
        var stringValues = values.Select(f =>
        {
            if (f == null) return string.Empty;

            return f switch
            {
                DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture), // ISO 8601 format
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => f.ToString()
            };
        });

        // Join all string values with a clear separator to avoid ambiguity
        string dataToSign = string.Join("|", stringValues);

        // Convert the combined string to bytes (UTF-8)
        byte[] dataBytes = Encoding.UTF8.GetBytes(dataToSign);

        // Compute SHA256 hash
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(dataBytes);

            // Return the hash as a Base64 string
            return Convert.ToBase64String(hashBytes);
        }
    }
}
