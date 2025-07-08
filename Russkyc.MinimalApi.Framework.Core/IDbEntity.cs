using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Russkyc.MinimalApi.Framework.Core;

public interface IDbEntity<TKeyType>
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TKeyType Id { get; set; }
}