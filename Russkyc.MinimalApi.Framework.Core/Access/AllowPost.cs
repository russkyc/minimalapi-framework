namespace Russkyc.MinimalApi.Framework.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AllowPost(params string[] permission) : Attribute
{
    public string[] Permission { get; } = permission;
}