using System;

namespace DataAccessClient.Exceptions
{
    public class DuplicateKeyException : Exception
    {
        public DuplicateKeyException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
