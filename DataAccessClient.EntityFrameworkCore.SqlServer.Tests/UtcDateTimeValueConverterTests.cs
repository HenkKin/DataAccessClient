using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Xunit;

namespace DataAccessClient.EntityFrameworkCore.SqlServer.Tests
{
    public class UtcDateTimeValueConverterTests
    {
        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ConvertFromProviderExpression_WhenDateTimeWithAnyKindIsProvided_ItShouldReturnDateTimeWithKindUtc(DateTimeKind dateTimeKind)
        {
            // Arrange
            var offset = dateTimeKind == DateTimeKind.Local ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);

            var input = new DateTimeOffset(2020, 2, 12, 22, 39, 35, offset);
            var dateTime = DateTime.SpecifyKind(input.DateTime, dateTimeKind);

            Assert.Equal(dateTimeKind, dateTime.Kind);

            var utcDateTimeValueConverter = new UtcDateTimeValueConverter(new ConverterMappingHints());

            // Act
            var result = utcDateTimeValueConverter.ConvertFromProviderExpression.Compile().Invoke(dateTime);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(input.UtcDateTime, result.Value);
            Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ConvertFromProviderExpression_WhenNullableDateTimeWithAnyKindIsProvided_ItShouldReturnDateTimeWithKindUtc(DateTimeKind dateTimeKind)
        {
            // Arrange
            var offset = dateTimeKind == DateTimeKind.Local ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);

            var input = new DateTimeOffset(2020, 2, 12, 22, 39, 35, offset);
            var dateTime = DateTime.SpecifyKind(input.DateTime, dateTimeKind);
            Assert.Equal(dateTimeKind, dateTime.Kind);

            var utcDateTimeValueConverter = new UtcDateTimeValueConverter(new ConverterMappingHints());

            // Act
            var result = utcDateTimeValueConverter.ConvertFromProviderExpression.Compile().Invoke(dateTime);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(input.UtcDateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ConvertToProviderExpression_WhenDateTimeWithAnyKindIsProvided_ItShouldReturnDateTimeWithKindUtc(DateTimeKind dateTimeKind)
        {
            // Arrange
            var offset = dateTimeKind == DateTimeKind.Local ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);

            var input = new DateTimeOffset(2020, 2, 12, 22, 39, 35, offset);
            var dateTime = DateTime.SpecifyKind(input.DateTime, dateTimeKind);
            Assert.Equal(dateTimeKind, dateTime.Kind);

            var utcDateTimeValueConverter = new UtcDateTimeValueConverter(new ConverterMappingHints());

            // Act
            var result = utcDateTimeValueConverter.ConvertToProviderExpression.Compile().Invoke(dateTime);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(input.UtcDateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void ConvertToProviderExpression_WhenNullableDateTimeWithAnyKindIsProvided_ItShouldReturnDateTimeWithKindUtc(DateTimeKind dateTimeKind)
        {
            // Arrange
            var offset = dateTimeKind == DateTimeKind.Local ? DateTimeOffset.Now.Offset : TimeSpan.FromHours(0);

            var input = new DateTimeOffset(2020, 2, 12, 22, 39, 35, offset);
            var dateTime = DateTime.SpecifyKind(input.DateTime, dateTimeKind);
            Assert.Equal(dateTimeKind, dateTime.Kind);

            var utcDateTimeValueConverter = new UtcDateTimeValueConverter(new ConverterMappingHints());

            // Act
            var result = utcDateTimeValueConverter.ConvertToProviderExpression.Compile().Invoke(dateTime);

            // Assert
            Assert.True(result.HasValue);
            Assert.Equal(input.UtcDateTime, result);
            Assert.Equal(DateTimeKind.Utc, result.Value.Kind);
        }


        [Fact]
        public void ConvertToProviderExpression_WhenNullIsProvided_ItShouldReturnNull()
        {
            // Arrange
            var utcDateTimeValueConverter = new UtcDateTimeValueConverter(new ConverterMappingHints());

            // Act
            var result = utcDateTimeValueConverter.ConvertToProviderExpression.Compile().Invoke(null);

            // Assert
            Assert.False(result.HasValue);
        }
    }
}