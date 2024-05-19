using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using Moq;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class InlineInvalidateTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/inlineinvalidate/";
        private readonly Mock<IApiOutputCache> _cache;

        public InlineInvalidateTests()
        {
            Thread.CurrentPrincipal = null;

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
        public async Task Inline_call_to_invalidate_is_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PostAsync(_url + "Post", stringContent))
            {
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.inlineinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Inline_call_to_invalidate_using_expression_tree_is_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PutAsync(_url + "Put", stringContent))
            {
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.inlineinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Inline_call_to_invalidate_using_expression_tree_with_param_is_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (_ = await client.DeleteAsync(_url + "Delete_parameterized"))
            {
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.inlineinvalidatecontroller-get_c100_s100_with_param")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Inline_call_to_invalidate_using_expression_tree_with_custom_action_name_is_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (_ = await client.DeleteAsync(_url + "Delete_non_standard_name"))
            {
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.inlineinvalidatecontroller-getbyid")),
                    Times.Exactly(1));
            }
        }
    }
}
