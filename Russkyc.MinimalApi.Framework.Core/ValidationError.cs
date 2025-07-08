namespace Russkyc.MinimalApi.Framework.Core;

public class ValidationError
{
    public required string Message { get; set; }
    public required IEnumerable<KeyValuePair<string, string[]>> Errors { get; set; }
}