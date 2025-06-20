using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Extensions;
using Russkyc.MinimalApi.Framework.Options;
using SampleProject;

// Configuration
FrameworkDbContextOptions.DbContextConfiguration = options =>
    options.UseSqlite("Data Source=test.sqlite");
FrameworkDbContextOptions.DbContextType = typeof(CustomDbContext);
FrameworkOptions.EnableRealtimeEvents = true;

var builder = WebApplication.CreateBuilder();
builder.Services.AddMinimalApiFramework();

builder.Build()
    .UseMinimalApiFramework()
    .Run();