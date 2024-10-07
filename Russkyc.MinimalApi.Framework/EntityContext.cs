using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework;

public class EntityContext<T> : DbContext, IEntityContext<T> where T : class
{
    public EntityContext(DbContextOptions<EntityContext<T>> options) : base(options)
    {
    }

    public DbSet<T> Entities { get; set; } = null!;

    public IQueryable<T> Set()
    {
        return Entities;
    }

    public async ValueTask<int> AddAsync(T model)
    {
        Entities.Add(model);
        return await SaveChangesAsync();
    }

    public async ValueTask<int> RemoveAsync(T model)
    {
        Entities.Remove(model);
        return await SaveChangesAsync();
    }

    public async ValueTask<int> UpdateAsync(T model)
    {
        return await SaveChangesAsync();
    }
}