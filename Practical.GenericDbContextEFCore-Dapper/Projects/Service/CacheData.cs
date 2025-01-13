using Practical.GenericDbContextEFCore.Service.Interface;
using System.Collections.Concurrent;

namespace Practical.GenericDbContextEFCore.Service
{
    public class CacheData : IDisposable, ICacheData
    {
        private ConcurrentDictionary<Type, object> cachedData;

        public CacheData()
        {
            cachedData = new ConcurrentDictionary<Type, object>();
        }

        /// <summary>
        /// Try to add data of T model object to cache
        /// </summary>
        /// <param name="sources"></param>
        /// <returns></returns>
        public bool TryAdd<T>(IList<T> sources)
        {
            try {
                if (!cachedData.ContainsKey(typeof(T))) {
                    cachedData.TryAdd(typeof(T), sources);
                    return true;
                }

                // Doesn't duplicate adding the same type while it's being already in cache. 
                // Try to get cache and do update it instead
                return false;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Try to get data based on type of a specific model
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public bool TryGet<T>(out IList<T> results)
        {
            try {
                if (cachedData.ContainsKey(typeof(T))) {
                    results = (IList<T>)cachedData[typeof(T)];
                    return true;
                }

                results = null;
                return false;
            }
            catch (Exception) {
                results = null;
                return false;
            }
        }

        /// <summary>
        /// Support add Type with any object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources"></param>
        /// <returns></returns>
        public bool TryAddSingleObj<T>(T source)
        {
            try {
                if (!cachedData.ContainsKey(typeof(T))) {
                    cachedData.TryAdd(typeof(T), source);
                    return true;
                }

                // Doesn't duplicate adding the same type while it's being already in cache. 
                // Try to get cache and do update it instead
                return false;
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Update data from cache to make sure cache will not obsolute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sources"></param>
        /// <returns></returns>
        public bool UpdateCache<T>(IList<T> sources)
        {
            cachedData.TryRemove(typeof(T), out _);
            return TryAdd(sources);
        }

        /// <summary>
        /// Try get value from specific type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool TryGetSingleObj<T>(out T result)
        {
            result = (T)cachedData.GetValueOrDefault(typeof(T));
            return result != null;
        }

        /// <summary>
        /// Check data of a specific type is whether existing in cache or not
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool Exist(Type type)
        {
            return cachedData.ContainsKey(type);
        }

        /// <summary>
        /// Get or set data to cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public IEnumerable<T> GetOrSet<T>(Func<IEnumerable<T>> func)
        {
            if (!TryGet<T>(out var ts)) {
                var result = func.Invoke();
                TryAdd(result.ToList());
                return result;
            }
            return ts;
        }

        /// <summary>
        /// Get or set data to cache
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        public IEnumerable<T> GetOrSet<T>(Func<Task<IEnumerable<T>>> func)
        {
            if (!TryGet<T>(out var ts)) {
                var result = Task.Run(async () => await func.Invoke()).Result;
                TryAdd(result.ToList());
                return result;
            }
            return ts;
        }

        /// <summary>
        /// Try to remove data of specific model based on it's type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public bool TryRemove(Type type)
        {
            try {
                return cachedData.ContainsKey(type) && cachedData.TryRemove(type, out _);
            }
            catch (Exception) {
                return false;
            }
        }

        /// <summary>
        /// Check empty or not
        /// </summary>
        /// <returns></returns>
        public bool IsEmpty()
        {
            if (cachedData != null) {
                return cachedData.IsEmpty;
            }
            return false;
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        public void Clear()
        {
            cachedData.Clear();
        }

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="types"></param>
        public void Clear(IEnumerable<Type> types)
        {
            if (types == null || !types.Any()) return;

            foreach (var key in types) {
                cachedData.TryRemove(key, out _);
            }
        }

        #region Disposable
        private bool _disposed = false;

        public async Task ForceClearCacheAsync()
        {
            await Task.Run(() => Dispose());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing) {
                cachedData = null;
            }

            _disposed = true;
        }

        ~CacheData()
        {
            Dispose(false);
        }
        #endregion
    }
}
