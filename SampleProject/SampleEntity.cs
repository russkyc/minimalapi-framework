using System.ComponentModel.DataAnnotations;
using Russkyc.MinimalApi.Framework.Core;

namespace SampleProject;

public class SampleEntity : DbEntity<Guid>
{
    [Required, MinLength(5)]
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity EmbeddedEntity { get; set; }
}