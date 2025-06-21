using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework.Server.Options;

public static class FrameworkDbContextOptions
{
    public static Type? DbContextType { get; set; } = null;
    public static ServiceLifetime DbContextLifetime { get; set; } = ServiceLifetime.Scoped;
    public static ServiceLifetime DbContextOptionsLifetime { get; set; } = ServiceLifetime.Scoped;
    
    public static Action<DbContextOptionsBuilder>? DbContextConfiguration { get; set; } = null;
    public static DatabaseAction DatabaseAction { get; set; } = DatabaseAction.EnsureCreated;
}