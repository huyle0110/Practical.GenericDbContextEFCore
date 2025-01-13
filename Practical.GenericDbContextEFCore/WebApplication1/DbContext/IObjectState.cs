using Practical.GenericDbContextEFCore.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public interface IObjectState
    {
        [NotMapped]
        ObjectState ObjectState { get; set; }
    }
}
