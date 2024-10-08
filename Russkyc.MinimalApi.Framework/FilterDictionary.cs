namespace Russkyc.MinimalApi.Framework;

public class FilterDictionary : Dictionary<string, (string Operation, string Value)>, IParsable<FilterDictionary>
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
                var key = keyValue[0];
                var value = keyValue[1];

                var operation = "EQUALS";
                if (value.StartsWith("CONTAINS(") && value.EndsWith(")"))
                {
                    operation = "CONTAINS";
                    value = value.Substring(9, value.Length - 10);
                }
                else if (value.StartsWith("STARTSWITH(") && value.EndsWith(")"))
                {
                    operation = "STARTSWITH";
                    value = value.Substring(11, value.Length - 12);
                }
                else if (value.StartsWith("ENDSWITH(") && value.EndsWith(")"))
                {
                    operation = "ENDSWITH";
                    value = value.Substring(9, value.Length - 10);
                }
                else if (value.StartsWith("GREATERTHAN(") && value.EndsWith(")"))
                {
                    operation = "GREATERTHAN";
                    value = value.Substring(12, value.Length - 13);
                }
                else if (value.StartsWith("LESSTHAN(") && value.EndsWith(")"))
                {
                    operation = "LESSTHAN";
                    value = value.Substring(9, value.Length - 10);
                }

                result[key] = (operation, value);
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