using System;

namespace WebApi.OutputCache.Core.Time
{
    public class ThisDay : IModelQuery<DateTime, CacheTime>
    {
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;

        public ThisDay(int hour, int minute, int second)
        {
            _hour = hour;
            _minute = minute;
            _second = second;
        }

        public CacheTime Execute(DateTime model)
        {
            var cacheTime = new CacheTime
            {
                AbsoluteExpiration = new DateTimeOffset(
                    new DateTime(
                        model.Year,
                        model.Month,
                        model.Day,
                        _hour,
                        _minute,
                        _second,
                        DateTimeKind.Unspecified)),
            };

            if (cacheTime.AbsoluteExpiration <= new DateTimeOffset(model))
            {
                cacheTime.AbsoluteExpiration = cacheTime.AbsoluteExpiration.AddDays(1);
            }

            cacheTime.ClientTimeSpan = cacheTime.AbsoluteExpiration.Subtract(new DateTimeOffset(model));

            return cacheTime;
        }
    }
}
