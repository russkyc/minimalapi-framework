namespace Russkyc.MinimalApi.Framework.Server.Extensions;

internal static class AttributeExtensions
{
    internal static TAttribute[] GetAttributeValue<TAttribute>(
        this Type type) 
        where TAttribute : Attribute
    {
        return type.GetCustomAttributes(
            typeof(TAttribute), true
        ) as TAttribute[] ?? [];
    }
}