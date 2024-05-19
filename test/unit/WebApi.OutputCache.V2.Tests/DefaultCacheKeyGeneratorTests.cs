using FluentAssertions;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public class DefaultCacheKeyGeneratorTests
        : CacheKeyGenerationTestsBase<DefaultCacheKeyGenerator>
    {
        public DefaultCacheKeyGeneratorTests()
        {
            CacheKeyGenerator = new DefaultCacheKeyGenerator();
        }

        [Fact]
        public void NoParametersIncludeQueryString_ShouldReturnBaseKeyAndQueryStringAndMediaTypeConcatenated()
        {
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: false);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{RequestUri.Query.Substring(1)}:{MediaType}",
                "Key does not match expected <BaseKey>-<QueryString>:<MediaType>");
        }

        [Fact]
        public void NoParametersExcludeQueryString_ShouldReturnBaseKeyAndMediaTypeConcatenated()
        {
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: true);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}:{MediaType}",
                "Key does not match expected <BaseKey>:<MediaType>");
        }

        [Fact]
        public void WithParametersIncludeQueryString_ShouldReturnBaseKeyAndArgumentsAndQueryStringAndMediaTypeConcatenated()
        {
            AddActionArgumentsToContext();
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: false);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{FormatActionArgumentsForKeyAssertion()}&{RequestUri.Query.Substring(1)}:{MediaType}",
                "Key does not match expected <BaseKey>-<Arguments>&<QueryString>:<MediaType>");
        }

        [Fact]
        public void WithParametersExcludeQueryString_ShouldReturnBaseKeyAndArgumentsAndMediaTypeConcatenated()
        {
            AddActionArgumentsToContext();
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: true);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{FormatActionArgumentsForKeyAssertion()}:{MediaType}",
                "Key does not match expected <BaseKey>-<Arguments>:<MediaType>");
        }
    }
}
