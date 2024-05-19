using System;

namespace WebApi.OutputCache.Core.Time
{
    public class ShortTime : IModelQuery<DateTime, CacheTime>
    {
        private readonly int _serverTimeInSeconds;
        private readonly int _clientTimeInSeconds;
        private readonly int? _sharedTimeInSecounds;

        public ShortTime(int serverTimeInSeconds, int clientTimeInSeconds, int? sharedTimeInSecounds)
        {
            if (serverTimeInSeconds < 0)
            {
                serverTimeInSeconds = 0;
            }

            _serverTimeInSeconds = serverTimeInSeconds;

            if (clientTimeInSeconds < 0)
            {
                clientTimeInSeconds = 0;
            }

            _clientTimeInSeconds = clientTimeInSeconds;

            if (sharedTimeInSecounds.HasValue && sharedTimeInSecounds.Value < 0)
            {
                sharedTimeInSecounds = 0;
            }

            _sharedTimeInSecounds = sharedTimeInSecounds;
        }

        public CacheTime Execute(DateTime model)
        {
            var cacheTime = new CacheTime
            {
                AbsoluteExpiration = new DateTimeOffset(model.AddSeconds(_serverTimeInSeconds)),
                ClientTimeSpan = TimeSpan.FromSeconds(_clientTimeInSeconds),
                SharedTimeSpan = _sharedTimeInSecounds.HasValue ? (TimeSpan?)TimeSpan.FromSeconds(_sharedTimeInSecounds.Value) : null,
            };

            return cacheTime;
        }
    }
}
