using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web.Http.Controllers;
using FluentAssertions;

namespace WebApi.OutputCache.V2.Tests
{
    /// <summary>
    /// Base class for implementing tests for the generation of cache keys (meaning: implementations of the <see cref="ICacheKeyGenerator"/>.
    /// </summary>
    public abstract class CacheKeyGenerationTestsBase<TCacheKeyGenerator>
        where TCacheKeyGenerator : ICacheKeyGenerator
    {
        private const string ArgumentKey = "filterExpression";
        private const string ArgumentValue = "val";

        protected CacheKeyGenerationTestsBase()
        {
            RequestUri = new Uri("http://localhost:8080/cacheKeyGeneration?filter=val");
            var controllerType = typeof(TestControllers.CacheKeyGenerationController);
            var actionMethodInfo = controllerType.GetMethod(
                nameof(TestControllers.CacheKeyGenerationController.Get),
                BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public,
                null,
                new Type[] { typeof(string) },
                null);
            var controllerDescriptor = new HttpControllerDescriptor { ControllerType = controllerType };
            var actionDescriptor = new ReflectedHttpActionDescriptor(controllerDescriptor, actionMethodInfo);
            var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, RequestUri.AbsoluteUri);

            Context = new HttpActionContext(
                new HttpControllerContext { ControllerDescriptor = controllerDescriptor, Request = request },
                actionDescriptor);
            MediaType = new MediaTypeHeaderValue("application/json");

            BaseCacheKey = new CacheOutputConfiguration(null).MakeBaseCacheKey(
                (TestControllers.CacheKeyGenerationController c) => c.Get(string.Empty));
        }

        protected HttpActionContext Context { get; set; }

        protected MediaTypeHeaderValue MediaType { get; set; }

        protected Uri RequestUri { get; set; }

        protected TCacheKeyGenerator CacheKeyGenerator { get; set; }

        protected string BaseCacheKey { get; set; }

        protected virtual void AssertCacheKeysBasicFormat(string cacheKey)
        {
            cacheKey.Should().NotBeNull();
            cacheKey.Should().StartWith(BaseCacheKey, "Key does not start with BaseKey");
            cacheKey.Should().EndWith(MediaType.ToString(), "Key does not end with MediaType");
        }

        protected void AddActionArgumentsToContext()
        {
            Context.ActionArguments.Add(ArgumentKey, ArgumentValue);
        }

        protected string FormatActionArgumentsForKeyAssertion() => $"{ArgumentKey}={ArgumentValue}";
    }
}
