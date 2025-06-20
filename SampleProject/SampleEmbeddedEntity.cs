using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Attributes;

namespace SampleProject;

[AllowPost("xcxs")]
public class SampleEmbeddedEntity : DbEntity<Guid>
{
    public string Property2 { get; set; }
}