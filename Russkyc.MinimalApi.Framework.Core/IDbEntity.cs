using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Russkyc.MinimalApi.Framework.Core;

public interface IDbEntity<TKeyType>
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public TKeyType Id { get; set; }
}