namespace Russkyc.MinimalApi.Framework;

public class FilterDictionary : Dictionary<string, string>, IParsable<FilterDictionary>
{
    public static FilterDictionary Parse(string s, IFormatProvider? provider)
    {
        var result = new FilterDictionary();
        var pairs = s.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                result[keyValue[0]] = keyValue[1];
            }
        }

        return result;
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out FilterDictionary result)
    {
        try
        {
            result = Parse(s ?? string.Empty, provider);
            return true;
        }
        catch
        {
            result = new FilterDictionary();
            return false;
        }
    }
}