using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Russkyc.MinimalApi.Framework.Core;

public abstract class DbEntity<TKeyType> : IDbEntity<TKeyType>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TKeyType Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}