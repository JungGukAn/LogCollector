using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;

namespace LogCollectorCore
{
    public static class ServiceExtentions
    {
        public static void AddLogCollector(this IServiceCollection services, Action<HttpLogCollectorOption> configure)
        {
            var defaultOptions = new HttpLogCollectorOption();
            configure?.Invoke(defaultOptions);

            if (defaultOptions.IsInvalid(out var reason))
            {
                throw new InvalidOperationException(reason);
            }

            services.AddSingleton(defaultOptions);
        }

        public static void UseLogCollector(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<LogCollectorMiddleware>();
        }
    }

    public static class LogDynamicObjectExtentions
    {
        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Add(this object log, string key, object value)
        {
            if (log is LogDynamicObject)
            {
                ((LogDynamicObject)log).Add(key, value);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool ContainsKey(this object log, string key)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).ContainsKey(key);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Remove(this object log, string key)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).Remove(key);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetValue(this object log, string key, out object value)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).TryGetValue(key, out value);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="item"></param>
        public static void Add(this object log, KeyValuePair<string, object> item)
        {
            if (log is LogDynamicObject)
            {
                ((LogDynamicObject)log).Add(item);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        public static void Clear(this object log)
        {
            if (log is LogDynamicObject)
            {
                ((LogDynamicObject)log).Clear();
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool Contains(this object log, KeyValuePair<string, object> item)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).Contains(item);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        public static void CopyTo(this object log, KeyValuePair<string, object>[] array, int arrayIndex)
        {
            if (log is LogDynamicObject)
            {
                ((LogDynamicObject)log).CopyTo(array, arrayIndex);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static bool Remove(this object log, KeyValuePair<string, object> item)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).Remove(item);
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }

        /// <summary>
        /// Only run in log.
        /// </summary>
        /// <param name="log"></param>
        /// <returns></returns>
        public static IEnumerator<KeyValuePair<string, object>> GetEnumerator(this object log)
        {
            if (log is LogDynamicObject)
            {
                return ((LogDynamicObject)log).GetEnumerator();
            }

            throw new NotSupportedException("Only run in LogDynamicObject type.");
        }
    }
}
