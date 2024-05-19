using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using FluentAssertions;
using Moq;
using WebApi.OutputCache.Core;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class ServerSideTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/sample/";
        private readonly Mock<IApiOutputCache> _cache;

        public ServerSideTests()
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
        public async Task Set_cache_to_predefined_valueAsync()
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
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        null),
                    Times.Once());
            }

            _cache.Verify(
                s => s.Add(
                    It.Is<string>(
                        x => x ==
                             "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:application/json; charset=utf-8"),
                    It.IsAny<object>(),
                    It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                    It.Is<string>(
                        x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                Times.Once());
        }

        [Fact]
        public async Task Set_cache_to_predefined_value_c100_s0Async()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c100_s0"))
            {
                // NOTE: Should we expect the _cache to not be called at all if the ServerTimeSpan is 0?
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s0:application/json; charset=utf-8")),
                    Times.Once());

                // NOTE: Server timespan is 0, so there should not have been any Add at all.
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s0"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        null),
                    Times.Never());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s0:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(1))),
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s0")),
                    Times.Never());
            }
        }

        [Fact]
        public async Task Not_cache_when_request_not_succesAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_httpResponseException_noCache"))
            {
                _cache.Verify(s => s.Contains(It.IsAny<string>()), Times.Once());
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Never());
            }
        }

        [Fact]
        public async Task Not_cache_when_request_exceptionAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_exception_noCache"))
            {
                _cache.Verify(s => s.Contains(It.IsAny<string>()), Times.Once());
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Never());
            }
        }

        [Fact]
        public async Task Not_cache_add_when_no_contentAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_noContent"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_request_nocontent:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Exactly(4));
            }
        }

        [Fact]
        public async Task Set_cache_to_predefined_value_respect_formatter_through_accept_headerAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_c100_s100"))
            {
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                using (var result = await client.SendAsync(req))
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
                                x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            null),
                        Times.Once());
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:text/xml; charset=utf-8"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                        Times.Once());
                }
            }
        }

        [Fact]
        public async Task Set_cache_to_predefined_value_respect_formatter_through_content_typeAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_c100_s100"))
            {
                req.Content = new StringContent(string.Empty);
                req.Content.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                using (var result = await client.SendAsync(req))
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
                                x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            null),
                        Times.Once());
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100:text/xml; charset=utf-8"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c100_s100")),
                        Times.Exactly(1));
                }
            }
        }

        [Fact]
        public async Task Set_cache_dont_exclude_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_false/1?xxx=2"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false-id=1&xxx=2:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false-id=1&xxx=2:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task
            Set_cache_dont_exclude_querystring_duplicate_action_arg_in_querystring_is_still_excludedAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_false/1?id=1"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false-id=1:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false-id=1:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_false")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Set_cache_do_exclude_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_true/1?xxx=1"))
            {
                // check
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true-id=1:application/json; charset=utf-8")),
                    Times.Exactly(2));

                // base
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());

                // actual
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true-id=1:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task
            Set_cache_do_exclude_querystring_do_not_exclude_action_arg_even_if_passed_as_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_true?id=1"))
            {
                // check
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true-id=1:application/json; charset=utf-8")),
                    Times.Exactly(2));

                // base
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());

                // actual
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true-id=1:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_true")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Callback_at_the_end_is_excluded_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_fakecallback?id=1&callback=abc"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Callback_at_the_beginning_is_excluded_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_fakecallback?callback=abc&id=1"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Callback_in_the_middle_is_excluded_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_fakecallback?de=xxx&callback=abc&id=1"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1&de=xxx:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback-id=1&de=xxx:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Callback_alone_is_excluded_querystringAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_exclude_fakecallback?callback=abc"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_s50_exclude_fakecallback")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task No_caching_if_user_authenticated_and_flag_set_to_offAsync()
        {
            SetCurrentThreadIdentity();
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_s50_c50_anonymousonly"))
            {
                result.IsSuccessStatusCode.Should().BeTrue();
                result.Headers.CacheControl.Should().BeNull();
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Never());
            }
        }

        [Fact]
        public async Task Etag_match_304_if_none_matchAsync()
        {
            _cache.Setup(x => x.Contains(It.Is<string>(i => i.Contains("etag_match_304")))).Returns(true);
            _cache.Setup(x => x.Get<string>(It.Is<string>(i => i.Contains("etag_match_304") && i.Contains(Constants.EtagKey))))
                  .Returns(@"""abc""");

            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "etag_match_304"))
            {
                req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(@"""abc"""));
                using (var result = await client.SendAsync(req))
                {
                    result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(50));
                    result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
                    result.StatusCode.Should().Be(HttpStatusCode.NotModified);
                }
            }
        }

        [Fact]
        public async Task Etag_not_match_304_if_none_matchAsync()
        {
            _cache.Setup(x => x.Contains(It.Is<string>(i => i.Contains("etag_match_304")))).Returns(true);
            _cache.Setup(x => x.Get<byte[]>(It.IsAny<string>())).Returns((byte[])null);
            _cache.Setup(x => x.Get<string>(It.Is<string>(i => i.Contains("etag_match_304") && i.Contains(Constants.EtagKey))))
                  .Returns(@"""abcdef""");

            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "etag_match_304"))
            {
                req.Headers.IfNoneMatch.Add(new EntityTagHeaderValue(@"""abc"""));
                using (var result = await client.SendAsync(req))
                {
                    result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(50));
                    result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
                    result.StatusCode.Should().Be(HttpStatusCode.OK);
                }
            }
        }

        [Fact]
        public async Task Can_handle_ihttpactionresult_with_default_media_typeAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_ihttpactionresult"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task Can_handle_ihttpactionresult_with_non_default_media_typeAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_ihttpactionresult"))
            {
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                using (var result = await client.SendAsync(req))
                {
                    _cache.Verify(
                        s => s.Contains(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult:text/xml; charset=utf-8")),
                        Times.Exactly(2));
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            null),
                        Times.Once());
                    _cache.Verify(
                        s => s.Add(
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult:text/xml; charset=utf-8"),
                            It.IsAny<object>(),
                            It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                            It.Is<string>(
                                x => x ==
                                     "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_ihttpactionresult")),
                        Times.Once());
                }
            }
        }

        [Fact]
        public async Task Can_handle_media_type_when_cache_has_expired_during_requestAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_ihttpactionresult"))
            {
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
                _cache.Setup(o => o.Contains(It.IsAny<string>())).Returns(true);
                _cache.Setup(
                        o => o.Get<MediaTypeHeaderValue>(It.Is((string key) => key.Contains(Constants.ContentTypeKey))))
                    .Returns((MediaTypeHeaderValue)null);
                using (var result = await client.SendAsync(req))
                {
                    result.IsSuccessStatusCode.Should().BeTrue();
                }
            }
        }

        [Fact]
        public async Task Will_cache_if_cacheouput_present_on_controllerAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync("http://www.strathweb.com/api/ignore/cached"))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.ignorecontroller-cached:application/json; charset=utf-8")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.ignorecontroller-cached"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.ignorecontroller-cached:application/json; charset=utf-8"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(100))),
                        It.Is<string>(x => x == "webapi.outputcache.v2.tests.testcontrollers.ignorecontroller-cached")),
                    Times.Once());
            }
        }

        [Fact]
        public async Task
            Will_not_cache_if_cacheouput_present_on_controller_but_action_has_ignorecacheouputattibuteAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync("http://www.strathweb.com/api/ignore/uncached"))
            {
                _cache.Verify(s => s.Contains(It.IsAny<string>()), Times.Never());
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Never());
                _cache.Verify(
                    s => s.Add(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<DateTimeOffset>(), It.IsAny<string>()),
                    Times.Never());
            }
        }

        [Fact]
        public async Task Override_mediatypeAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Get_c50_s50_image"))
            using (var result = await client.SendAsync(req))
            {
                _cache.Verify(
                    s => s.Contains(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c50_s50_image:image/jpeg")),
                    Times.Exactly(2));
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x == "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c50_s50_image"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        null),
                    Times.Once());
                _cache.Verify(
                    s => s.Add(
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c50_s50_image:image/jpeg"),
                        It.IsAny<object>(),
                        It.Is<DateTimeOffset>(x => x <= new DateTimeOffset(DateTime.Now.AddSeconds(50))),
                        It.Is<string>(
                            x => x ==
                                 "webapi.outputcache.v2.tests.testcontrollers.samplecontroller-get_c50_s50_image")),
                    Times.Once());
                result.Content.Headers.ContentType.Should().Be(new MediaTypeHeaderValue("image/jpeg"));
            }
        }

        private static void SetCurrentThreadIdentity()
        {
            var customIdentity = new Mock<IIdentity>();
            customIdentity.SetupGet(x => x.IsAuthenticated).Returns(true);
            var threadCurrentPrincipal = new GenericPrincipal(customIdentity.Object, new string[] { "CustomUser" });
            Thread.CurrentPrincipal = threadCurrentPrincipal;
        }
    }
}
