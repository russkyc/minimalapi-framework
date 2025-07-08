using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Core.Attributes;
using Russkyc.MinimalApi.Framework.Extensions;
using Russkyc.MinimalApi.Framework.Options;

var builder = WebApplication.CreateBuilder();

// Configure
FrameworkOptions.MapIndexToApiDocs = true;
FrameworkDbContextOptions.DbContextConfiguration = options => options.UseSqlite("Data Source=test.sqlite");

// Add required services
builder.Services
    .AddMinimalApiFramework();

var webApplication = builder.Build();

// Add required endpoints
// Optionally, you can disable entity endpoints mapping and map them manually
webApplication.UseMinimalApiFramework(mapEntityEndpoints: false);

// Manual entity endpoints mapping for more granular control
webApplication.MapEntityEndpoints<SampleEntity, Guid>(options =>
{
    // Other endpoint options can be configured here
    // This is an example of adding a custom filter to the SampleEntity endpoints
    options.AddEndpointFilter(async (context, next) =>
    {
        Console.WriteLine("Executing action: " + context.HttpContext.Request.Method + " " + context.HttpContext.Request.Path);
        var result = await next(context);
        return result;
    });
});

// Sample prefixed mapping
// Same effect can be achieved when using the minimal setup
// by using the `FrameworkOptions` and setting `ApiPrefix`
var apiGroup = webApplication.MapGroup("nested");
apiGroup.MapEntityEndpoints<SampleEmbeddedEntity, int>();

await webApplication.RunAsync();

[RequirePermission(ApiMethod.Post,"xcx")]
[RequirePermission(ApiMethod.Get, "xcv")]
public class SampleEmbeddedEntity : DbEntity<int>
{
    public required string Property2 { get; set; }
}

public class SampleEntity : DbEntity<Guid>
{
    [Required, MinLength(5)]
    public required string Property { get; set; }
    public virtual SampleEmbeddedEntity? EmbeddedEntity { get; set; }
}