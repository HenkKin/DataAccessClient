using System;
using DataAccessClient.Exceptions;
using Xunit;

namespace DataAccessClient.Tests.Exceptions
{
    public class CriteriaTests
    {
        [Fact]
        public void WhenExceptionIsCreated_ItShouldPassMessageAndInnerExceptionToBaseException()
        {
            // Arrange
            var message = "custom message";
            var innerException = new Exception("inner exception");
            // Act
            var exception = new DuplicateKeyException(message, innerException);

            // Assert
            Assert.Equal(message, exception.Message);
            Assert.Same(innerException, exception.InnerException);
            Assert.Equal(innerException.Message, exception.InnerException.Message);
        }
    }
}
