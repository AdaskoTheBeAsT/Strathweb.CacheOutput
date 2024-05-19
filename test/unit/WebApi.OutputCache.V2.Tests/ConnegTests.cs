using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Moq;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class ConnegTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/sample/";
        private readonly Mock<IApiOutputCache> _cache;

        public ConnegTests()
        {
            _cache = new Mock<IApiOutputCache>();

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
            _server.Dispose();
            _configuration.Dispose();
            _container.Dispose();
        }

        [Fact]
        public async Task Subsequent_xml_request_is_not_cachedAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c100_s100"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x < new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                    Times.Once());

                using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_c100_s100"))
                {
                    req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));

                    using (var result2 = await client.SendAsync(req))
                    {
                        _cache.Verify(
                            s => s.Contains(
                                It.Is<string>(
                                    x => x ==
                                         "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:text/xml; charset=utf-8")),
                            Times.Exactly(2));
                        _cache.Verify(
                            s => s.Add(
                                It.Is<string>(
                                    x => x ==
                                         "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:text/xml; charset=utf-8"),
                                It.IsAny<object>(),
                                It.Is<DateTimeOffset>(x => x < new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                                It.Is<string>(
                                    x => x ==
                                         "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                            Times.Once());
                    }
                }
            }
        }
    }
}
