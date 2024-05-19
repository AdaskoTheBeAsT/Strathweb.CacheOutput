using System;

namespace WebApi.OutputCache.Core.Time
{
    public class ThisMonth : IModelQuery<DateTime, CacheTime>
    {
        private readonly int _day;
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;

        public ThisMonth(int day, int hour, int minute, int second)
        {
            _day = day;
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
                        _day,
                        _hour,
                        _minute,
                        _second,
                        DateTimeKind.Unspecified)),
            };

            if (cacheTime.AbsoluteExpiration <= new DateTimeOffset(model))
            {
                cacheTime.AbsoluteExpiration = cacheTime.AbsoluteExpiration.AddMonths(1);
            }

            cacheTime.ClientTimeSpan = cacheTime.AbsoluteExpiration.Subtract(new DateTimeOffset(model));

            return cacheTime;
        }
    }
}
