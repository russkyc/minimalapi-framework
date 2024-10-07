using System.ComponentModel.DataAnnotations;
using Russkyc.MinimalApi.Framework;

namespace SampleProject;

[DbEntity]
public class SampleEmbeddedEntity
{
    [Key]
    public int Id { get; set; }
    public string Property2 { get; set; }
}