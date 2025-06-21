using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Russkyc.MinimalApi.Framework.Core;

#pragma warning disable CS8618 // Id is not a required property but it is generated so we can assume it will be set by the database.
public abstract class DbEntity<TKeyType> : IDbEntity<TKeyType>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public TKeyType Id { get; set; }
}