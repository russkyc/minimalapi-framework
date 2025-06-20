namespace Russkyc.MinimalApi.Framework.Extensions;

internal static class AttributeExtensions
{
    internal static TAttribute? GetAttributeValue<TAttribute>(
        this Type type) 
        where TAttribute : Attribute
    {
        if (type.GetCustomAttributes(
                typeof(TAttribute), true
            ).FirstOrDefault() is TAttribute attribute)
        {
            return attribute;
        }
        return null;
    }
}