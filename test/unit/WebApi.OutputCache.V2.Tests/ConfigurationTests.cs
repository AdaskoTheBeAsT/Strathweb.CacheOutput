using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using Moq;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class ConfigurationTests
    {
        private readonly string _url = "http://www.strathweb.com/api/sample/";
        private Mock<IApiOutputCache> _cache;

        [Fact]
        public async Task Cache_singleton_in_pipelineAsync()
        {
            _cache = new Mock<IApiOutputCache>();

            using (var configuration = new HttpConfiguration())
            {
                configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => _cache.Object);

                configuration.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{action}/{id}",
                    defaults: new { id = RouteParameter.Optional });

                using (var server = new HttpServer(configuration))
                using (var client = new HttpClient(server))
                using (var result = await client.GetAsync(_url + "Get_c100_s100"))
                {
                    _cache.Verify(
                        s => s.Contains(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:application/json; charset=utf-8")),
                        Times.Exactly(2));

                    using (var result2 = await client.GetAsync(_url + "Get_c100_s100"))
                    {
                        _cache.Verify(
                            s => s.Contains(
                                It.Is<string>(
                                    x => x ==
                                         "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:application/json; charset=utf-8")),
                            Times.Exactly(4));
                    }
                }
            }
        }

        [Fact]
        public void Cache_singleton()
        {
            var cache = new MemoryCacheDefault();

            using (var conf = new HttpConfiguration())
            {
                conf.CacheOutputConfiguration().RegisterCacheOutputProvider(() => cache);

                conf.Properties.TryGetValue(typeof(IApiOutputCache), out var cache1);

                conf.Properties.TryGetValue(typeof(IApiOutputCache), out var cache2);
                var obj1 = ((Func<IApiOutputCache>)cache1)();
                var obj2 = ((Func<IApiOutputCache>)cache2)();

                obj1.Should().Be(obj2);
            }
        }

        [Fact]
        public void Cache_instance()
        {
            using (var conf = new HttpConfiguration())
            {
                conf.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MemoryCacheDefault());

                conf.Properties.TryGetValue(typeof(IApiOutputCache), out var cache1);

                conf.Properties.TryGetValue(typeof(IApiOutputCache), out var cache2);

                var obj1 = ((Func<IApiOutputCache>)cache1)();
                var obj2 = ((Func<IApiOutputCache>)cache2)();

                obj1.Should().NotBe(obj2);
            }
        }
    }
}
