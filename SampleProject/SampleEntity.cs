using System.ComponentModel.DataAnnotations;
using Russkyc.MinimalApi.Framework;

namespace SampleProject;

[DbEntity]
public class SampleEntity
{
    [Key]
    public int Id { get; set; }
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity EmbeddedEntity { get; set; }
}