namespace Practical.GenericDbContextEFCore.Service.Interface
{
    public interface ICacheData
    {
        /// <summary>
        /// Try to add data of T model object to cache
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        bool TryAdd<T>(IList<T> sources);

        /// <summary>
        /// Try to get data based on type of a specific model
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        bool TryGet<T>(out IList<T> results);

        bool TryRemove(Type type);

        /// <summary>
        /// Check data of a specific type is whether existing in cache or not
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        bool Exist(Type type);

        /// <summary>
        /// Check empty or not
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty();

        /// <summary>
        /// Clear cache
        /// </summary>
        void Clear();

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="types"></param>
        void Clear(IEnumerable<Type> types);

        /// <summary>
        /// Force to clear cache data
        /// </summary>
        /// <returns></returns>
        Task ForceClearCacheAsync();

        bool TryAddSingleObj<T>(T source);
        bool TryGetSingleObj<T>(out T result);
        bool UpdateCache<T>(IList<T> sources);
        IEnumerable<T> GetOrSet<T>(Func<IEnumerable<T>> func);
        IEnumerable<T> GetOrSet<T>(Func<Task<IEnumerable<T>>> func);
    }
}
