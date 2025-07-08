using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Access;
using Russkyc.MinimalApi.Framework.Core.Attributes;
using Russkyc.MinimalApi.Framework.Options;

FrameworkApiDocsOptions.EnableSidebar = true;

MinimalApiFramework
    .CreateDefault(options => options.UseSqlite("Data Source=test.sqlite"))
    .Run();

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