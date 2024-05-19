using System;

namespace WebApi.OutputCache.Core.Time
{
    public class ThisYear : IModelQuery<DateTime, CacheTime>
    {
        private readonly int _month;
        private readonly int _day;
        private readonly int _hour;
        private readonly int _minute;
        private readonly int _second;

        public ThisYear(int month, int day, int hour, int minute, int second)
        {
            _month = month;
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
                        _month,
                        _day,
                        _hour,
                        _minute,
                        _second,
                        DateTimeKind.Unspecified)),
            };

            if (cacheTime.AbsoluteExpiration <= new DateTimeOffset(model))
            {
                cacheTime.AbsoluteExpiration = cacheTime.AbsoluteExpiration.AddYears(1);
            }

            cacheTime.ClientTimeSpan = cacheTime.AbsoluteExpiration.Subtract(new DateTimeOffset(model));

            return cacheTime;
        }
    }
}
