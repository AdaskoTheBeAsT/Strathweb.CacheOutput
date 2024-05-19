using System;
using System.Runtime.Serialization;

namespace WebApi.OutputCache.V2
{
    [Serializable]
    public class SharedTimeSpanShouldNotBeCalledWithoutValueSetException
        : Exception
    {
        public SharedTimeSpanShouldNotBeCalledWithoutValueSetException()
        {
        }

        public SharedTimeSpanShouldNotBeCalledWithoutValueSetException(string message)
            : base(message)
        {
        }

        public SharedTimeSpanShouldNotBeCalledWithoutValueSetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected SharedTimeSpanShouldNotBeCalledWithoutValueSetException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}
