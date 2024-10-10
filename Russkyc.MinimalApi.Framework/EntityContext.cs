using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Russkyc.MinimalApi.Framework.Core;

namespace Russkyc.MinimalApi.Framework;

public sealed class EntityContext<T> : DbContext, IEntityContext<T> where T : class
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
    
    public void CreateDatabase()
    {
        Database.EnsureCreated();
    }

    public void DeleteDatabase()
    {
        Database.EnsureDeleted();
    }
}