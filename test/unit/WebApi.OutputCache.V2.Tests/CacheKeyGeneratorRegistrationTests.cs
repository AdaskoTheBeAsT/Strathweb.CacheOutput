using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using Autofac;
using Autofac.Integration.WebApi;
using Moq;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class CacheKeyGeneratorRegistrationTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/";
        private readonly Mock<IApiOutputCache> _cache;
        private readonly Mock<ICacheKeyGenerator> _keyGenerator;

        public CacheKeyGeneratorRegistrationTests()
        {
            Thread.CurrentPrincipal = null;

            _cache = new Mock<IApiOutputCache>();
            _keyGenerator = new Mock<ICacheKeyGenerator>();

            _configuration = new HttpConfiguration();

            var builder = new ContainerBuilder();
            builder.RegisterInstance(_cache.Object);
            _container = builder.Build();

            _configuration.DependencyResolver = new AutofacWebApiDependencyResolver(_container);
            _configuration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional });

            _server = new HttpServer(_configuration);
        }

        public void Dispose()
        {
            _server?.Dispose();
            _configuration?.Dispose();
            _container?.Dispose();
        }

        [Fact]
        public async Task Registered_default_is_usedAsync()
        {
            _server.Configuration.CacheOutputConfiguration().RegisterDefaultCacheKeyGeneratorProvider(() => _keyGenerator.Object);

            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "sample/Get_c100_s100"))
            {
                _keyGenerator.VerifyAll();
            }
        }

        [Fact]
        public async Task Last_registered_default_is_usedAsync()
        {
            _server.Configuration.CacheOutputConfiguration().RegisterDefaultCacheKeyGeneratorProvider(() =>
            {
                Assert.Fail("First registration should have been overwritten");
                return null;
            });
            _server.Configuration.CacheOutputConfiguration().RegisterDefaultCacheKeyGeneratorProvider(() => _keyGenerator.Object);

            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "sample/Get_c100_s100"))
            {
                _keyGenerator.VerifyAll();
            }
        }

        [Fact]
        public async Task Specific_registration_does_not_affect_defaultAsync()
        {
            _server.Configuration.CacheOutputConfiguration().RegisterDefaultCacheKeyGeneratorProvider(() => _keyGenerator.Object);
            _server.Configuration.CacheOutputConfiguration().RegisterCacheKeyGeneratorProvider(() => new FailCacheKeyGenerator());

            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "sample/Get_c100_s100"))
            {
                _keyGenerator.VerifyAll();
            }
        }

        [Fact]
        public async Task Selected_generator_with_internal_registration_is_usedAsync()
        {
            _server.Configuration.CacheOutputConfiguration()
                .RegisterCacheKeyGeneratorProvider(() => new InternalRegisteredCacheKeyGenerator("internal"));

            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "cachekey/get_internalregistered"))
            {
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(x => x == "internal"),
                        It.IsAny<byte[]>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.cachekeycontroller-get_internalregistered")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Custom_unregistered_cache_key_generator_calledAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "cachekey/get_unregistered"))
            {
                _cache.Verify(s => s.Contains(It.Is<string>(x => x == "unregistered")), Times.Once());
            }
        }

        public class InternalRegisteredCacheKeyGenerator
            : ICacheKeyGenerator
        {
            private readonly string _key;

            public InternalRegisteredCacheKeyGenerator(string key)
            {
                _key = key;
            }

            public string MakeCacheKey(HttpActionContext context, MediaTypeHeaderValue mediaType, bool excludeQueryString = false)
            {
                return _key;
            }
        }

        private class FailCacheKeyGenerator
            : ICacheKeyGenerator
        {
            public string MakeCacheKey(HttpActionContext context, MediaTypeHeaderValue mediaType, bool excludeQueryString = false)
            {
                Assert.Fail("This cache key generator should never be invoked");
                return "fail";
            }
        }
    }
}
