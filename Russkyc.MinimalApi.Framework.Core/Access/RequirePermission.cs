using Russkyc.MinimalApi.Framework.Core.Access;

namespace Russkyc.MinimalApi.Framework.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequirePermission(ApiMethod method,params string[] permission) : Attribute
{
    public ApiMethod Method { get; set; } = method;
    public string[] Permission { get; } = permission;
}