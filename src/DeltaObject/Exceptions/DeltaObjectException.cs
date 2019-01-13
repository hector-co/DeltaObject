using System;

namespace DeltaObject.Exceptions
{
    public class DeltaObjectException : Exception
    {
        public DeltaObjectException()
        {
        }

        public DeltaObjectException(string message) : base(message)
        {
        }

        public DeltaObjectException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
