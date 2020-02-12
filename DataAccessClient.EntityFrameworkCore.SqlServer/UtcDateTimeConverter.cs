using System;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace DataAccessClient.EntityFrameworkCore.SqlServer
{
    internal class UtcDateTimeValueConverter : ValueConverter<DateTime?, DateTime?>
    {
        public UtcDateTimeValueConverter(ConverterMappingHints mappingHints = null) : base(ConvertToUtcExpression, ConvertToUtcExpression, mappingHints)
        {
        }

        private static readonly Expression<Func<DateTime?, DateTime?>> ConvertToUtcExpression = dateTime => dateTime.HasValue ? ConvertToUtc(dateTime.Value) : dateTime;

        public static DateTime ConvertToUtc(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }

            if (dateTime.Kind == DateTimeKind.Local)
            {
                return dateTime.ToUniversalTime();
            }

            return dateTime;
        }
    }
}
