using System;
using System.Collections.Generic;
using WebApi.OutputCache.Core.Cache;

namespace WebApi.OutputCache.V2.Tests
{
    public class SimpleCacheForTests
        : IApiOutputCache
    {
        private readonly Dictionary<string, object> _cachedItems;

        public SimpleCacheForTests()
        {
            _cachedItems = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual IEnumerable<string> AllKeys => _cachedItems.Keys;

        public virtual void RemoveStartsWith(string key)
        {
            throw new NotSupportedException();
        }

        public virtual T Get<T>(string key)
            where T : class
        {
            var o = _cachedItems[key] as T;
            return o;
        }

        public virtual void Remove(string key)
        {
            _cachedItems.Remove(key);
        }

        public virtual bool Contains(string key)
        {
            return _cachedItems.ContainsKey(key);
        }

        public virtual void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            _cachedItems.Add(key, o);
        }
    }
}
