using System;
using FluentAssertions;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.Core.Tests
{
    public class MemoryCacheDefaultTests
    {
        [Fact]
        public void Returns_all_keys_in_cache()
        {
            IApiOutputCache cache = new MemoryCacheDefault();
            cache.Add("base", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)));
            cache.Add("key1", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");
            cache.Add("key2", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");
            cache.Add("key3", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");

            var result = cache.AllKeys;

            result.Should().BeEquivalentTo("base", "key1", "key2", "key3");
        }

        [Fact]
        public void Remove_startswith_cascades_to_all_dependencies()
        {
            IApiOutputCache cache = new MemoryCacheDefault();
            cache.Add("base", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)));
            cache.Add("key1", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");
            cache.Add("key2", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");
            cache.Add("key3", "abc", new DateTimeOffset(DateTime.Now.AddSeconds(60)), "base");
            cache.Get<string>("key1").Should().NotBeNull();
            cache.Get<string>("key2").Should().NotBeNull();
            cache.Get<string>("key3").Should().NotBeNull();

            cache.RemoveStartsWith("base");

            cache.Get<string>("base").Should().BeNull();
            cache.Get<string>("key1").Should().BeNull();
            cache.Get<string>("key2").Should().BeNull();
            cache.Get<string>("key3").Should().BeNull();
        }
    }
}
