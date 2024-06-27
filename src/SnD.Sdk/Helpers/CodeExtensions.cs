using System.Text;
using System.Text.Json;

namespace Snd.Sdk.Helpers;

/// <summary>
/// Generic code pieces for every day use.
/// </summary>
public static class CodeExtensions
{
    /// <summary>
    /// Generated code.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static string CamelCaseToSnakeCase(string input)
    {
        var output = new StringBuilder();
        foreach (var ch in input)
        {
            if (char.IsUpper(ch))
            {
                if (output.Length > 0 && !char.IsUpper(output[^1]))
                {
                    output.Append('_');
                }
                output.Append(char.ToLower(ch));
            }
            else
            {
                output.Append(ch);
            }
        }
        return output.ToString();
    }

    /// <summary>
    /// Forces an object to be cloned through json serialization - deserialization, thus a completely new object is returned as a result of this operation.
    /// </summary>
    /// <param name="entity">Any object that is JSON serializable. Note this method has no compile-time guardrails, so ensure you provide JSON-serializable.</param>
    /// <typeparam name="T">Type of the entity to clone.</typeparam>
    /// <returns></returns>
    public static T DeepClone<T>(this T entity) => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(entity));
}
