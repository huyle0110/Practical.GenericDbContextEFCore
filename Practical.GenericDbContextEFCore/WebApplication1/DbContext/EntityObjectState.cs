using Practical.GenericDbContextEFCore.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public abstract class EntityObjectState : IObjectState
    {
        [NotMapped]
        public ObjectState ObjectState { get; set; }
        [NotMapped]
        public IList<string> ChangedFields { get; set; } = new List<string>();
    }
}
