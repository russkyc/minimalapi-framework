namespace Russkyc.MinimalApi.Framework.Core;

public class ValidationError
{
    public string Message { get; set; }
    public IDictionary<string,string[]> Errors { get; set; }
}