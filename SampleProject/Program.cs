using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;

MinimalApiFramework
    .CreateDefault(options => options.UseSqlite("Data Source=test.sqlite"))
    .Run();