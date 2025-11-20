namespace Russkyc.MinimalApi.Framework.Core.Access;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequireRoles(ApiMethod method, params string[] roles) : Attribute
{
    public ApiMethod Method { get; set; } = method;
    public string[] Roles { get; } = roles;
}
