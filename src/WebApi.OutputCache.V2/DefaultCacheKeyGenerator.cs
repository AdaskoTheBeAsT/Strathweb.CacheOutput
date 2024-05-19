using System;
using System.Collections;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web.Http.Controllers;

namespace WebApi.OutputCache.V2
{
    public class DefaultCacheKeyGenerator : ICacheKeyGenerator
    {
        public virtual string MakeCacheKey(HttpActionContext context, MediaTypeHeaderValue mediaType, bool excludeQueryString = false)
        {
            var key = MakeBaseKey(context);
            var parameters = FormatParameters(context, excludeQueryString);

            return $"{key}{parameters}:{mediaType}";
        }

        protected virtual string MakeBaseKey(HttpActionContext context)
        {
            var controller = context.ControllerContext.ControllerDescriptor.ControllerType.FullName;
            var action = context.ActionDescriptor.ActionName;
            using (var configuration = context.Request.GetConfiguration())
            {
                return configuration.CacheOutputConfiguration().MakeBaseCacheKey(controller, action);
            }
        }

        protected virtual string FormatParameters(HttpActionContext context, bool excludeQueryString)
        {
            var actionParameters = context.ActionArguments.Where(x => x.Value != null).Select(x => x.Key + "=" + GetValue(x.Value));

            string parameters;

            if (!excludeQueryString)
            {
                var queryStringParameters =
                    context.Request.GetQueryNameValuePairs()
                           .Where(x => !x.Key.Equals("callback", StringComparison.OrdinalIgnoreCase))
                           .Select(x => x.Key + "=" + x.Value);
                var parametersCollections = actionParameters.Union(queryStringParameters, StringComparer.OrdinalIgnoreCase);
                parameters = "-" + string.Join("&", parametersCollections);

                var callbackValue = GetJsonpCallback(context.Request);
                if (!string.IsNullOrWhiteSpace(callbackValue))
                {
                    var callback = "callback=" + callbackValue;
                    if (parameters.Contains("&" + callback))
                    {
                        parameters = parameters.Replace("&" + callback, string.Empty);
                    }

                    if (parameters.Contains(callback + "&"))
                    {
                        parameters = parameters.Replace(callback + "&", string.Empty);
                    }

                    if (parameters.Contains("-" + callback))
                    {
                        parameters = parameters.Replace("-" + callback, string.Empty);
                    }

                    if (parameters.EndsWith("&", StringComparison.OrdinalIgnoreCase))
                    {
                        parameters = parameters.TrimEnd('&');
                    }
                }
            }
            else
            {
                parameters = "-" + string.Join("&", actionParameters);
            }

            if (parameters.Equals("-"))
            {
                parameters = string.Empty;
            }

            return parameters;
        }

        private string GetJsonpCallback(HttpRequestMessage request)
        {
            var callback = string.Empty;
            if (request.Method == HttpMethod.Get)
            {
                var query = request.GetQueryNameValuePairs();

                if (query != null)
                {
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
                    foreach (var keyValuePair in query)
                    {
                        if (keyValuePair.Key.Equals(nameof(callback), StringComparison.OrdinalIgnoreCase))
                        {
                            callback = keyValuePair.Value;
                            break;
                        }
                    }
#pragma warning restore S3267 // Loops should be simplified with "LINQ" expressions
                }
            }

            return callback;
        }

        private string GetValue(object val)
        {
            if (val is IEnumerable && !(val is string))
            {
                var concatValue = string.Empty;
                var paramArray = val as IEnumerable;
                return paramArray.Cast<object>().Aggregate(concatValue, (current, paramValue) => current + (paramValue + ";"));
            }

            return val.ToString();
        }
    }
}
