﻿using Microsoft.EntityFrameworkCore;

namespace Russkyc.MinimalApi.Framework.Data;

public class BaseDbContext : DbContext
{
    public BaseDbContext(DbContextOptions options) : base(options)
    {
        
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