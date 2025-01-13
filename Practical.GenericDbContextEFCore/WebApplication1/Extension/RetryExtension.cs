using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Practical.GenericDbContextEFCore.Extension
{
    public static class RetryExtension
    {
        /// <summary>
        /// execute retry
        /// </summary>
        /// <param name="action"></param>
        /// <param name="retryInterval"></param>
        /// <param name="maxAttemptCount"></param>
        /// <param name="actionWhenException"></param>
        public static void DoRetry(Action action, Func<Exception, bool> exceptionType, TimeSpan retryInterval, int maxAttemptCount = 3, Action<int> actionWhenException = null)
        {
            DoRetryReturnResult<object>(() =>
            {
                action();
                return null;
            }, exceptionType, retryInterval, maxAttemptCount, actionWhenException);
        }

        /// <summary>
        /// execute retry
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="retryInterval"></param>
        /// <param name="maxAttemptCount"></param>
        /// <param name="actionWhenException"></param>
        /// <returns></returns>
        public static T DoRetryReturnResult<T>(Func<T> action, Func<Exception, bool> exceptionType, TimeSpan retryInterval, int maxAttemptCount = 3, Action<int> actionWhenException = null)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        Task.Delay(retryInterval).Wait();
                    }
                    return action();
                }
                catch (Exception ex)
                {
                    actionWhenException?.Invoke(attempted + 1);
                    if (exceptionType.Invoke(ex))
                    {
                        exceptions.Add(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new AggregateException(exceptions);
        }

        /// <summary>
        /// DoRetryReturnResultAsync Support Async Await
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="exceptionType"></param>
        /// <param name="retryInterval"></param>
        /// <param name="maxAttemptCount"></param>
        /// <param name="actionWhenException"></param>
        /// <returns></returns>
        /// <exception cref="AggregateException"></exception>
        public static async Task<T> DoRetryReturnResultAsync<T>(Func<Task<T>> action, Func<Exception, bool> exceptionType, TimeSpan retryInterval, int maxAttemptCount = 3, Action<int> actionWhenException = null)
        {
            var exceptions = new List<Exception>();

            for (int attempted = 0; attempted < maxAttemptCount; attempted++)
            {
                try
                {
                    if (attempted > 0)
                    {
                        await Task.Delay(retryInterval);
                    }
                    return await action();
                }
                catch (Exception ex)
                {
                    actionWhenException?.Invoke(attempted + 1);
                    if (exceptionType.Invoke(ex))
                    {
                        exceptions.Add(ex);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            throw new AggregateException(exceptions);
        }
    }
}
