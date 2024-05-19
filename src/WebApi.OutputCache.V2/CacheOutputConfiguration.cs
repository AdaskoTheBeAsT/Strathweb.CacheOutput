using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using WebApi.OutputCache.Core.Cache;

namespace WebApi.OutputCache.V2
{
    public class CacheOutputConfiguration(HttpConfiguration configuration)
    {
        public void RegisterCacheOutputProvider(Func<IApiOutputCache> provider)
        {
            configuration.Properties.GetOrAdd(typeof(IApiOutputCache), _ => provider);
        }

        public void RegisterCacheKeyGeneratorProvider<T>(Func<T> provider)
            where T : ICacheKeyGenerator
        {
            configuration.Properties.GetOrAdd(typeof(T), _ => provider);
        }

        public void RegisterDefaultCacheKeyGeneratorProvider(Func<ICacheKeyGenerator> provider)
        {
            RegisterCacheKeyGeneratorProvider(provider);
        }

        public string MakeBaseCacheKey(string controller, string action)
        {
            return $"{controller.ToLower(CultureInfo.InvariantCulture)}-{action.ToLower(CultureInfo.InvariantCulture)}";
        }

        public string MakeBaseCacheKey<T, TU>(Expression<Func<T, TU>> expression)
        {
            if (expression.Body is not MethodCallExpression method)
            {
                throw new ArgumentException("Expression is wrong", nameof(expression));
            }

            var methodName = method.Method.Name;
            var nameAttributes = method.Method.GetCustomAttributes(typeof(ActionNameAttribute), inherit: false);
            if (nameAttributes.Length > 0)
            {
                var actionNameAttrib = (ActionNameAttribute)nameAttributes.FirstOrDefault();
                if (actionNameAttrib != null)
                {
                    methodName = actionNameAttrib.Name;
                }
            }

            return $"{typeof(T).FullName?.ToLower(CultureInfo.InvariantCulture)}-{methodName.ToLower(CultureInfo.InvariantCulture)}";
        }

        public ICacheKeyGenerator GetCacheKeyGenerator(HttpRequestMessage request, Type generatorType)
        {
            generatorType ??= typeof(ICacheKeyGenerator);
            configuration.Properties.TryGetValue(generatorType, out var cache);

#pragma warning disable IDISP004 // Don't ignore created IDisposable
            var generator = cache is Func<ICacheKeyGenerator> cacheFunc
                ? cacheFunc()
                : request.GetDependencyScope().GetService(generatorType) as ICacheKeyGenerator;
#pragma warning restore IDISP004 // Don't ignore created IDisposable

            return generator
                ?? TryActivateCacheKeyGenerator(generatorType)
                ?? new DefaultCacheKeyGenerator();
        }

        public IApiOutputCache GetCacheOutputProvider(HttpRequestMessage request)
        {
            configuration.Properties.TryGetValue(typeof(IApiOutputCache), out var cache);

#pragma warning disable IDISP004 // Don't ignore created IDisposable
            var cacheOutputProvider = cache is Func<IApiOutputCache> cacheFunc
                ? cacheFunc()
                : request.GetDependencyScope().GetService(typeof(IApiOutputCache)) as IApiOutputCache ?? new MemoryCacheDefault();
#pragma warning restore IDISP004 // Don't ignore created IDisposable

            return cacheOutputProvider;
        }

        private static ICacheKeyGenerator TryActivateCacheKeyGenerator(Type generatorType)
        {
#pragma warning disable S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
#pragma warning disable S6603 // The collection-specific "TrueForAll" method should be used instead of the "All" extension
            var hasEmptyOrDefaultConstructor =
                generatorType.GetConstructor(Type.EmptyTypes) != null ||
                generatorType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                    .Any(x => x.GetParameters().All(p => p.IsOptional));
#pragma warning restore S6603 // The collection-specific "TrueForAll" method should be used instead of the "All" extension
#pragma warning restore S6605 // Collection-specific "Exists" method should be used instead of the "Any" extension
            return hasEmptyOrDefaultConstructor
                ? Activator.CreateInstance(generatorType) as ICacheKeyGenerator
                : null;
        }
    }
}
