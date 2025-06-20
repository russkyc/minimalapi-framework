namespace Russkyc.MinimalApi.Framework.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AllowGet(params string[] permission) : Attribute
{
    public string[] Permission { get; } = permission;
}