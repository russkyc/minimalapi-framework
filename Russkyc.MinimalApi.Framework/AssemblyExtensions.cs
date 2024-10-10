using System.Reflection;

namespace Russkyc.MinimalApi.Framework;

public static class AssemblyExtensions
{
    public static string GetAssemblyName(this Assembly assembly)
    {
        return assembly.GetName().Name!;
    }
}