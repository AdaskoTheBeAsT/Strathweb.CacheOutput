using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using FluentAssertions;
using WebApi.OutputCache.Core.Cache;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class CustomHeadersTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly IContainer _container;
        private readonly string _url = "http://www.strathweb.com/api/customheaders/";
        private readonly IApiOutputCache _cache;

        public CustomHeadersTests()
        {
            Thread.CurrentPrincipal = null;

            _cache = new SimpleCacheForTests();

            _configuration = new HttpConfiguration();
            var builder = new ContainerBuilder();
            builder.RegisterInstance(_cache);
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
        public async Task Cache_custom_content_headerAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Content_Header"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Content_Header"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Content.Headers.ContentDisposition.DispositionType.Should().Be("attachment");
                result2.Content.Headers.ContentDisposition.DispositionType.Should().Be("attachment");
            }
        }

        [Fact]
        public async Task Cache_custom_content_header_with_multiply_valuesAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Content_Header_Multiply_Values"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Content_Header_Multiply_Values"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result.Content.Headers.ContentEncoding.Last().Should().Be("gzip");

                result2.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result2.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result2.Content.Headers.ContentEncoding.Last().Should().Be("gzip");
            }
        }

        [Fact]
        public async Task Cache_custom_response_headerAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Response_Header"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Response_Header"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Headers.GetValues("RequestHeader1").First().Should().Be("value1");
                result2.Headers.GetValues("RequestHeader1").First().Should().Be("value1");
            }
        }

        [Fact]
        public async Task Cache_custom_response_header_with_multiply_valuesAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Response_Header_Multiply_Values"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Custom_Response_Header_Multiply_Values"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Headers.GetValues("RequestHeader2").Should().HaveCount(2);
                result.Headers.GetValues("RequestHeader2").First().Should().Be("value2");
                result.Headers.GetValues("RequestHeader2").Last().Should().Be("value3");

                result2.Headers.GetValues("RequestHeader2").Should().HaveCount(2);
                result2.Headers.GetValues("RequestHeader2").First().Should().Be("value2");
                result2.Headers.GetValues("RequestHeader2").Last().Should().Be("value3");
            }
        }

        [Fact]
        public async Task Cache_multiply_custom_headersAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Multiply_Custom_Headers"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Multiply_Custom_Headers"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Content.Headers.ContentDisposition.DispositionType.Should().Be("attachment");
                result.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result.Content.Headers.ContentEncoding.Last().Should().Be("gzip");
                result.Headers.GetValues("RequestHeader1").First().Should().Be("value1");
                result.Headers.GetValues("RequestHeader2").Should().HaveCount(2);
                result.Headers.GetValues("RequestHeader2").First().Should().Be("value2");
                result.Headers.GetValues("RequestHeader2").Last().Should().Be("value3");

                result2.Content.Headers.ContentDisposition.DispositionType.Should().Be("attachment");
                result2.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result2.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result2.Content.Headers.ContentEncoding.Last().Should().Be("gzip");
                result2.Headers.GetValues("RequestHeader1").First().Should().Be("value1");
                result2.Headers.GetValues("RequestHeader2").Should().HaveCount(2);
                result2.Headers.GetValues("RequestHeader2").First().Should().Be("value2");
                result2.Headers.GetValues("RequestHeader2").Last().Should().Be("value3");
            }
        }

        [Fact]
        public async Task Cache_part_of_custom_headersAsync()
        {
            using (var client = new HttpClient(_server))
            using (var req = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Part_Of_Custom_Headers"))
            using (var result = await client.SendAsync(req))
            using (var req2 = new HttpRequestMessage(HttpMethod.Get, _url + "Cache_Part_Of_Custom_Headers"))
            using (var result2 = await client.SendAsync(req2))
            {
                result.Content.Headers.ContentDisposition.DispositionType.Should().Be("attachment");
                result.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result.Content.Headers.ContentEncoding.Last().Should().Be("gzip");
                result.Headers.GetValues("RequestHeader1").First().Should().Be("value1");
                result.Headers.GetValues("RequestHeader2").Should().HaveCount(2);
                result.Headers.GetValues("RequestHeader2").First().Should().Be("value2");
                result.Headers.GetValues("RequestHeader2").Last().Should().Be("value3");

                result2.Content.Headers.ContentDisposition.Should().BeNull();
                result2.Content.Headers.ContentEncoding.Should().HaveCount(2);
                result2.Content.Headers.ContentEncoding.First().Should().Be("deflate");
                result2.Content.Headers.ContentEncoding.Last().Should().Be("gzip");

                result2.Headers.TryGetValues("RequestHeader1", out var _).Should().BeFalse();
                result2.Headers.TryGetValues("RequestHeader2", out var _).Should().BeFalse();
            }
        }
    }
}
