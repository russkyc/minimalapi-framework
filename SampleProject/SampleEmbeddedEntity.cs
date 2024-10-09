using Russkyc.MinimalApi.Framework.Core;

namespace SampleProject;

public class SampleEmbeddedEntity : DbEntity<Guid>
{
    public string Property2 { get; set; }
}