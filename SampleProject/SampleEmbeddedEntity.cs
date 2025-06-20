using Russkyc.MinimalApi.Framework.Core;
using Russkyc.MinimalApi.Framework.Core.Attributes;

namespace SampleProject;

[AllowPost("xcx")]
public class SampleEmbeddedEntity : DbEntity<int>
{
    public string Property2 { get; set; }
}