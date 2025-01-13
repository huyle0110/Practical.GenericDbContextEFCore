using System.Linq.Expressions;

namespace Practical.GenericDbContextEFCore.DbContext
{
    public interface IChangedDataContext
    {
        void UpdateEntity<T>(T entity) where T : class;
        void UpdateEntity<T>(T entity, params Expression<Func<T, object>>[] properties) where T : class;
        void UpdateRangeEntity<T>(IEnumerable<T> entities) where T : class;
    }
}
