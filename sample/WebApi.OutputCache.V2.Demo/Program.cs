using System;
using System.Web.Http;
using System.Web.Http.SelfHost;
using WebApi.OutputCache.Core.Cache;

namespace WebApi.OutputCache.V2.Demo
{
    internal static class Program
    {
        private static void Main()
        {
            using (var config = new HttpSelfHostConfiguration("http://localhost:999"))
            {
                config.MapHttpAttributeRoutes();
                config.Routes.MapHttpRoute(
                    name: "DefaultApi",
                    routeTemplate: "api/{controller}/{id}",
                    defaults: new { id = RouteParameter.Optional });
                using (var server = new HttpSelfHostServer(config))
                {
                    config.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new MemoryCacheDefault());

#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                    server.OpenAsync().GetAwaiter().GetResult();

                    Console.ReadKey();

                    server.CloseAsync().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
                }
            }
        }
    }
}
