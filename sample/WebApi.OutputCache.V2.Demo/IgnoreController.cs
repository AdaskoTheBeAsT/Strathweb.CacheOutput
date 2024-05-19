using System;
using System.Globalization;
using System.Web.Http;

namespace WebApi.OutputCache.V2.Demo
{
    [CacheOutput(ClientTimeSpan = 50, ServerTimeSpan = 50)]
    [RoutePrefix("ignore")]
    public class IgnoreController : ApiController
    {
        [Route("cached")]
        public string GetCached()
        {
            return DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }

        [IgnoreCacheOutput]
        [Route("uncached")]
        public string GetUnCached()
        {
            return DateTime.Now.ToString(CultureInfo.InvariantCulture);
        }
    }
}
