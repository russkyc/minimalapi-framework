using Russkyc.MinimalApi.Framework.Core;

namespace SampleProject;

public class SampleEntity : DbEntity<Guid>
{
    public string Property { get; set; }
    public virtual SampleEmbeddedEntity EmbeddedEntity { get; set; }
}