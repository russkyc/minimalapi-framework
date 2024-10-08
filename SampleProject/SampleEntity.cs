using Russkyc.MinimalApi.Framework;

namespace SampleProject;

public class SampleEntity : DbEntity<Guid>
{
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity EmbeddedEntity { get; set; }
}