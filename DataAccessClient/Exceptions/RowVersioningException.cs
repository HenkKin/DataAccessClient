using System;

namespace DataAccessClient.Exceptions
{
    public class RowVersioningException : Exception
    {
        public RowVersioningException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}