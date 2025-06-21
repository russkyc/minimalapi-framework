using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Russkyc.MinimalApi.Framework.Server;
using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Attributes;

MinimalApiFramework
    .CreateDefault(options => options.UseSqlite("Data Source=test.sqlite"))
    .Run();

[AllowPost("xcx")]
public class SampleEmbeddedEntity : DbEntity<int>
{
    public string Property2 { get; set; }
}

public class SampleEntity : DbEntity<Guid>
{
    [Required, MinLength(5)]
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity? EmbeddedEntity { get; set; }
}