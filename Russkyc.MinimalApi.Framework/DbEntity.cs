using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Russkyc.MinimalApi.Framework;

public abstract class DbEntity<TKeyType> : IDbEntity<TKeyType>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public TKeyType Id { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
}