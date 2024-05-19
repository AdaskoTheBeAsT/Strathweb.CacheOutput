using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Web.Http;
using WebApi.OutputCache.Core.Cache;

namespace WebApi.OutputCache.V2
{
    public class CacheOutputConfiguration
    {
        private readonly HttpConfiguration _configuration;

        public CacheOutputConfiguration(HttpConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void RegisterCacheOutputProvider(Func<IApiOutputCache> provider)
        {
            _configuration.Properties.GetOrAdd(typeof(IApiOutputCache), x => provider);
        }

        public void RegisterCacheKeyGeneratorProvider<T>(Func<T> provider)
            where T : ICacheKeyGenerator
        {
            _configuration.Properties.GetOrAdd(typeof(T), x => provider);
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
            var method = expression.Body as MethodCallExpression;
            if (method == null)
            {
                throw new ArgumentException("Expression is wrong", nameof(expression));
            }

            var methodName = method.Method.Name;
            var nameAttribs = method.Method.GetCustomAttributes(typeof(ActionNameAttribute), false);
            if (nameAttribs.Any())
            {
                var actionNameAttrib = (ActionNameAttribute)nameAttribs.FirstOrDefault();
                if (actionNameAttrib != null)
                {
                    methodName = actionNameAttrib.Name;
                }
            }

            return $"{typeof(T).FullName.ToLower(CultureInfo.InvariantCulture)}-{methodName.ToLower(CultureInfo.InvariantCulture)}";
        }

        public ICacheKeyGenerator GetCacheKeyGenerator(HttpRequestMessage request, Type generatorType)
        {
            generatorType ??= typeof(ICacheKeyGenerator);
            _configuration.Properties.TryGetValue(generatorType, out var cache);

            ICacheKeyGenerator generator;
            if (cache is Func<ICacheKeyGenerator> cacheFunc)
            {
                generator = cacheFunc();
            }
            else
            {
                using (var scope = request.GetDependencyScope())
                {
                    generator = scope.GetService(generatorType) as ICacheKeyGenerator;
                }
            }

            return generator
                ?? TryActivateCacheKeyGenerator(generatorType)
                ?? new DefaultCacheKeyGenerator();
        }

        public IApiOutputCache GetCacheOutputProvider(HttpRequestMessage request)
        {
            _configuration.Properties.TryGetValue(typeof(IApiOutputCache), out var cache);

            IApiOutputCache cacheOutputProvider;
            if (cache is Func<IApiOutputCache> cacheFunc)
            {
                cacheOutputProvider = cacheFunc();
            }
            else
            {
                using (var scope = request.GetDependencyScope())
                {
                    cacheOutputProvider = scope.GetService(typeof(IApiOutputCache)) as IApiOutputCache ?? new MemoryCacheDefault();
                }
            }

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
