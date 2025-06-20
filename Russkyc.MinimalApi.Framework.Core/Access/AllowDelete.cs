namespace Russkyc.MinimalApi.Framework.Core.Access;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AllowDelete(params string[] permission) : Attribute
{
    public string[] Permission { get; } = permission;
}