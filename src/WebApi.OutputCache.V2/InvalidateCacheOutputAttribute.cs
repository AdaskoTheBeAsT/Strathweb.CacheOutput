using System;
using System.Net.Http;
using System.Web.Http.Filters;

namespace WebApi.OutputCache.V2
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public sealed class InvalidateCacheOutputAttribute
        : BaseCacheAttribute
    {
        private readonly string _methodName;
        private string _controller;

        public InvalidateCacheOutputAttribute(string methodName)
            : this(methodName, null)
        {
        }

        public InvalidateCacheOutputAttribute(string methodName, Type type)
        {
            _controller = type != null ? type.FullName : null;
            _methodName = methodName;
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext.Response != null && !actionExecutedContext.Response.IsSuccessStatusCode)
            {
                return;
            }

            _controller = _controller ?? actionExecutedContext.ActionContext.ControllerContext.ControllerDescriptor.ControllerType.FullName;

            using (var config = actionExecutedContext.Request.GetConfiguration())
            {
                EnsureCache(config, actionExecutedContext.Request);

                var key = config.CacheOutputConfiguration()
                    .MakeBaseCacheKey(_controller, _methodName);
                if (WebApiCache.Contains(key))
                {
                    WebApiCache.RemoveStartsWith(key);
                }
            }
        }
    }
}
