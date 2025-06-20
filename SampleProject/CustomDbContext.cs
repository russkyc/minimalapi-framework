using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;
using Russkyc.MinimalApi.Framework.Data;

namespace SampleProject;

public class CustomDbContext : BaseDbContext
{
    public CustomDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<SampleEmbeddedEntity> SampleEmbeddedEntityCollection { get; set; }
    public DbSet<SampleEntity> SampleEntityCollection { get; set; }
}