using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using FluentAssertions;
using WebApi.OutputCache.Core.Time;
using Xunit;

namespace WebApi.OutputCache.V2.Tests
{
    public sealed class ClientSideTests
        : IDisposable
    {
        private readonly HttpServer _server;
        private readonly HttpConfiguration _configuration;
        private readonly string _url = "http://www.strathweb.com/api/sample/";

        public ClientSideTests()
        {
            _configuration = new HttpConfiguration();
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
        }

        [Fact]
        public async Task Maxage_mustrevalidate_false_headers_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c100_s100"))
            {
                result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(100));
                result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
            }
        }

        [Fact]
        public async Task No_cachecontrol_when_clienttimeout_is_zeroAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c0_s100"))
            {
                result.Headers.CacheControl.Should().BeNull();
            }
        }

        [Fact]
        public async Task No_cachecontrol_when_request_not_successAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_httpResponseException_noCache"))
            {
                result.Headers.CacheControl.Should().BeNull();
            }
        }

        [Fact]
        public async Task No_cachecontrol_when_request_exceptionAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_exception_noCache"))
            {
                result.Headers.CacheControl.Should().BeNull();
            }
        }

        [Fact]
        public async Task Maxage_cachecontrol_when_no_contentAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_request_noContent"))
            {
                result.Headers.CacheControl.Should().NotBeNull();
                result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(50));
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_clienttimeout_zero_with_must_revalidateAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c0_s100_mustR"))
            {
                result.Headers.CacheControl.MustRevalidate.Should().BeTrue();
                result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.Zero);
            }
        }

        [Fact]
        public async Task Nocache_headers_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_nocache"))
            {
                result.Headers.CacheControl.NoCache.Should().BeTrue(
                    "NoCache in result headers was expected to be true when CacheOutput.NoCache=true.");
                result.Headers.Contains("Pragma").Should().BeTrue("result headers does not contain expected Pragma.");
                result.Headers.GetValues("Pragma").Contains("no-cache", StringComparer.OrdinalIgnoreCase).Should()
                    .BeTrue("expected no-cache Pragma was not found");
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_true_headers_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c50_mustR"))
            {
                result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(50));
                result.Headers.CacheControl.MustRevalidate.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Maxage_private_true_headers_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c50_private"))
            {
                result.Headers.CacheControl.MaxAge.Should().Be(TimeSpan.FromSeconds(50));
                result.Headers.CacheControl.Private.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_cacheuntilAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_until25012100_1700"))
            {
                var clientTimeSpanSeconds = new SpecificTime(2100, 01, 25, 17, 0, 0).Execute(DateTime.Now)
                    .ClientTimeSpan.TotalSeconds;
                var resultCacheControlSeconds = ((TimeSpan)result.Headers.CacheControl.MaxAge).TotalSeconds;
                Math.Round(clientTimeSpanSeconds - resultCacheControlSeconds).Should().Be(0);
                result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_cacheuntil_todayAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_until2355_today"))
            {
                Math.Round(
                    new ThisDay(23, 55, 59).Execute(DateTime.Now).ClientTimeSpan.TotalSeconds -
                    ((TimeSpan)result.Headers.CacheControl.MaxAge).TotalSeconds).Should().Be(0);
                result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_cacheuntil_this_monthAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_until27_thismonth"))
            {
                Math.Round(
                    new ThisMonth(27, 0, 0, 0).Execute(DateTime.Now).ClientTimeSpan.TotalSeconds -
                    ((TimeSpan)result.Headers.CacheControl.MaxAge).TotalSeconds).Should().Be(0);
                result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_cacheuntil_this_yearAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_until731_thisyear"))
            {
                Math.Round(
                    new ThisYear(7, 31, 0, 0, 0).Execute(DateTime.Now).ClientTimeSpan.TotalSeconds -
                    ((TimeSpan)result.Headers.CacheControl.MaxAge).TotalSeconds).Should().Be(0);
                result.Headers.CacheControl.MustRevalidate.Should().BeFalse();
            }
        }

        [Fact]
        public async Task Maxage_mustrevalidate_headers_correct_with_cacheuntil_this_year_with_revalidateAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_until731_thisyear_mustrevalidate"))
            {
                Math.Round(
                    new ThisYear(7, 31, 0, 0, 0).Execute(DateTime.Now).ClientTimeSpan.TotalSeconds -
                    ((TimeSpan)result.Headers.CacheControl.MaxAge).TotalSeconds).Should().Be(0);
                result.Headers.CacheControl.MustRevalidate.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Private_true_headers_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_private"))
            {
                result.Headers.CacheControl.Private.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Shared_max_age_header_correctAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c100_s100_sm200"))
            {
                result.Headers.CacheControl.SharedMaxAge.Should().Be(TimeSpan.FromSeconds(200));
            }
        }

        [Fact]
        public async Task Shared_max_age_header_not_presentAsync()
        {
            using (var client = new HttpClient(_server))
            using (var result = await client.GetAsync(_url + "Get_c100_s100"))
            {
                result.Headers.CacheControl.SharedMaxAge.Should().BeNull();
            }
        }
    }
}
