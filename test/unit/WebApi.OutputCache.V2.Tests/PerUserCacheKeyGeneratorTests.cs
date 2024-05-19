using System;
using System.Globalization;
using System.Security.Principal;
using FluentAssertions;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public class PerUserCacheKeyGeneratorTests
        : CacheKeyGenerationTestsBase<PerUserCacheKeyGenerator>
    {
        private const string UserIdentityName = "SomeUserIDon'tMind";

        public PerUserCacheKeyGeneratorTests()
        {
            CacheKeyGenerator = new PerUserCacheKeyGenerator();
            Context.RequestContext.Principal = new GenericPrincipal(new GenericIdentity(UserIdentityName), Array.Empty<string>());
        }

        [Fact]
        public void NoParametersIncludeQueryString_ShouldReturnBaseKeyAndQueryStringAndUserIdentityAndMediaTypeConcatenated()
        {
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: false);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{RequestUri.Query.Substring(1)}:{FormatUserIdentityForAssertion()}:{MediaType}",
                "Key does not match expected <BaseKey>-<QueryString>:<UserIdentity>:<MediaType>");
        }

        [Fact]
        public void NoParametersExcludeQueryString_ShouldReturnBaseKeyAndUserIdentityAndMediaTypeConcatenated()
        {
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: true);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}:{FormatUserIdentityForAssertion()}:{MediaType}",
                "Key does not match expected <BaseKey>:<UserIdentity>:<MediaType>");
        }

        [Fact]
        public void WithParametersIncludeQueryString_ShouldReturnBaseKeyAndArgumentsAndQueryStringAndUserIdentityAndMediaTypeConcatenated()
        {
            AddActionArgumentsToContext();
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: false);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{FormatActionArgumentsForKeyAssertion()}&{RequestUri.Query.Substring(1)}:{FormatUserIdentityForAssertion()}:{MediaType}",
                "Key does not match expected <BaseKey>-<Arguments>&<QueryString>:<UserIdentity>:<MediaType>");
        }

        [Fact]
        public void WithParametersExcludeQueryString_ShouldReturnBaseKeyAndArgumentsAndUserIdentityAndMediaTypeConcatenated()
        {
            AddActionArgumentsToContext();
            var cacheKey = CacheKeyGenerator.MakeCacheKey(Context, MediaType, excludeQueryString: true);

            AssertCacheKeysBasicFormat(cacheKey);
            cacheKey.Should().Be(
                $"{BaseCacheKey}-{FormatActionArgumentsForKeyAssertion()}:{FormatUserIdentityForAssertion()}:{MediaType}",
                "Key does not match expected <BaseKey>-<Arguments>:<UserIdentity>:<MediaType>");
        }

        private string FormatUserIdentityForAssertion() => UserIdentityName.ToLower(CultureInfo.InvariantCulture);
    }
}
