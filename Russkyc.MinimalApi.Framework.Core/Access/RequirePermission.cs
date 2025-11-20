namespace Russkyc.MinimalApi.Framework.Core.Access;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermission(ApiMethod method,params string[] permission) : Attribute
{
    public ApiMethod Method { get; set; } = method;
    public string[] Permission { get; } = permission;
}