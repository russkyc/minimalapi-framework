using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Data;

namespace Russkyc.MinimalApi.Framework.Extensions;

public static class DbContextExtensions
{
    public static DbSet<TEntity> DbSet<TEntity>(this BaseDbContext context) where TEntity : class
    {
        var type = context.GetType();
        var property = type.GetProperty($"{typeof(TEntity).Name}Collection");
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8603 // Possible null reference return.
        return (DbSet<TEntity>)property.GetValue(context);
#pragma warning restore CS8603 // Possible null reference return.
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
    }
}