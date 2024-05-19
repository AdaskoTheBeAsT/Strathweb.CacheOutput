using System;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using FluentAssertions;
using Moq;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class InvalidateTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/sample/";
        private readonly Mock<IApiOutputCache> _cache;

        public InvalidateTests()
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
        public async Task Regular_invalidate_works_on_postAsync()
        {
            SetupCacheForAutoInvalidate();
            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PostAsync(_url + "Post", stringContent))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Regular_invalidate_on_two_methods_works_on_postAsync()
        {
            SetupCacheForAutoInvalidate();
            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PostAsync(_url + "Post_2_invalidates", stringContent))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Controller_level_invalidate_on_three_methods_works_on_postAsync()
        {
            SetupCacheForAutoInvalidate();

            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PostAsync(
                       "http://www.strathweb.com/api/autoinvalidate/Post",
                       stringContent))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Controller_level_invalidate_on_three_methods_works_on_putAsync()
        {
            SetupCacheForAutoInvalidate();

            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (_ = await client.PutAsync(
                       "http://www.strathweb.com/api/autoinvalidate/Put",
                       stringContent))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Controller_level_invalidate_on_three_methods_works_on_deleteAsync()
        {
            SetupCacheForAutoInvalidate();
            using (var client = new HttpClient(_server))
            using (_ = await client.DeleteAsync("http://www.strathweb.com/api/autoinvalidate/Delete"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304")),
                    Times.Exactly(1));
            }
        }

        [Fact]
        public async Task Controller_level_invalidate_with_type_check_does_not_invalidate_on_no_type_matchAsync()
        {
            SetupCacheForAutoInvalidate();
            using (var client = new HttpClient(_server))
            using (var stringContent = new StringContent(string.Empty))
            using (var result2 = await client.PostAsync(
                       "http://www.strathweb.com/api/autoinvalidatewithtype/Post",
                       stringContent))
            {
                result2.IsSuccessStatusCode.Should().BeTrue();
                _cache.Verify(s => s.Contains(It.IsAny<string>()), Times.Never());
                _cache.Verify(s => s.RemoveStartsWith(It.IsAny<string>()), Times.Never());
            }
        }

        [Fact]
        public async Task Controller_level_invalidate_with_type_check_invalidates_only_methods_with_types_matchedAsync()
        {
            SetupCacheForAutoInvalidate();
            using (var client = new HttpClient(_server))
            using (_ = await client.PostAsync(
                       "http://www.strathweb.com/api/autoinvalidatewithtype/PostString",
                       "hi",
                       new JsonMediaTypeFormatter()))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100_array")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_s50_exclude_fakecallback")),
                    Times.Never());
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100_array")),
                    Times.Exactly(1));
                _cache.Verify(
                    s => s.RemoveStartsWith(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_s50_exclude_fakecallback")),
                    Times.Never());
            }
        }

        private void SetupCacheForAutoInvalidate()
        {
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_c100_s100"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-get_s50_exclude_fakecallback"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatecontroller-etag_match_304"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_s50_exclude_fakecallback"))).Returns(true);
            _cache.Setup(x => x.Contains(It.Is<string>(s => s == "webapi.outputcache.v2.tests.testcontrollers.autoinvalidatewithtypecontroller-get_c100_s100_array"))).Returns(true);
        }
    }
}
