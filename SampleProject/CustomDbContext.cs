using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

namespace SampleProject;

public class CustomDbContext : BaseDbContext
{
    public CustomDbContext(DbContextOptions options) : base(options)
    {
    }
    
    public DbSet<SampleEmbeddedEntity> SampleEmbeddedEntityCollection { get; set; }
    public DbSet<SampleEntity> SampleEntityCollection { get; set; }
}