using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Data;

namespace Russkyc.MinimalApi.Framework.Extensions;

public static class DbContextExtensions
{
    public static DbSet<TEntity> DbSet<TEntity>(this BaseDbContext context) where TEntity : class
    {
        var type = context.GetType();
        var property = type.GetProperty($"{typeof(TEntity).Name}Collection");
        return (property!.GetValue(context) as DbSet<TEntity>)!;
    }
}