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
    public sealed class CacheKeyGeneratorTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/";
        private readonly Mock<IApiOutputCache> _cache;
        private readonly Mock<ICacheKeyGenerator> _keyGeneratorA;
        private readonly CustomCacheKeyGenerator _keyGeneratorB;

        public CacheKeyGeneratorTests()
        {
            Thread.CurrentPrincipal = null;

            _cache = new Mock<IApiOutputCache>();
            _keyGeneratorA = new Mock<ICacheKeyGenerator>();
            _keyGeneratorB = new CustomCacheKeyGenerator();

            _configuration = new HttpConfiguration();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_cache.Object);

            // this should become the default cache key generator
            builder.RegisterInstance(_keyGeneratorA.Object).As<ICacheKeyGenerator>();
            builder.RegisterInstance(_keyGeneratorB);
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
        public async Task Custom_default_cache_key_generator_called_and_key_usedAsync()
        {
            using (var client = new HttpClient(_server))
            {
                _keyGeneratorA.Setup(
                        k => k.MakeCacheKey(
                            It.IsAny<HttpActionContext>(),
                            It.IsAny<MediaTypeHeaderValue>(),
                            It.IsAny<bool>()))
                    .Returns("keykeykey")
                    .Verifiable("Key generator was never called");

                // use the samplecontroller to show that no changes are required to existing code
                using (var result = await client.GetAsync(_url + "sample/Get_c100_s100"))
                {
                    _cache.Verify(s => s.Contains(It.Is<string>(x => x == "keykeykey")), Times.Exactly(2));
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(x => x == "keykeykey"),
                            It.IsAny<byte[]>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                        Times.Once());
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(x => x == "keykeykey:response-ct"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                        Times.Once());

                    _keyGeneratorA.VerifyAll();
                }
            }
        }

        [Fact]
        public async Task Custom_cache_key_generator_calledAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "cachekey/get_custom_key"))
            {
                _cache.Verify(s => s.Contains(It.Is<string>(x => x == "custom_key")), Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(x => x == "custom_key"),
                        It.IsAny<byte[]>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.cachekeycontroller-get_custom_key")),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(x => x == "custom_key:response-ct"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.cachekeycontroller-get_custom_key")),
                    Times.Once());
            }
        }

        public class CustomCacheKeyGenerator
            : ICacheKeyGenerator
        {
            public string MakeCacheKey(HttpActionContext context, MediaTypeHeaderValue mediaType, bool excludeQueryString = false)
            {
                return "custom_key";
            }
        }
    }
}
