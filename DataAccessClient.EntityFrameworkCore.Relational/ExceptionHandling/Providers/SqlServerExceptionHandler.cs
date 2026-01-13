using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Reflection;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling.Providers
{
    public sealed class SqlServerExceptionHandler : ISaveChangesDbUpdateExceptionHandler
    {
        // Bekende SqlException FullName's (zowel oude als nieuwe provider).
        private static readonly string[] SqlExceptionFullNames =
        {
            "System.Data.SqlClient.SqlException",
            "Microsoft.Data.SqlClient.SqlException"
        };

        public bool TryParse(DbUpdateException ex, out DbExceptionInfo info)
        {
            info = default!;
            var sqlEx = ExceptionChain.Traverse(ex)
                .FirstOrDefault(e => SqlExceptionFullNames.Contains(e.GetType().FullName));

            if (sqlEx is null) return false;

            var number = GetSqlErrorNumber(sqlEx) ?? GetFromErrorsCollection(sqlEx);
            var kind = Classify(number);

            info = new DbExceptionInfo
            {
                Kind = kind,
                Message = sqlEx.Message,
                Provider = "SqlServer",
                Number = number,
                ClientConnectionId = GetGuidProperty(sqlEx, "ClientConnectionId"),
                OriginalException = ex
            };
            return true;
        }

        private static DbErrorKind Classify(int? number) => number switch
        {
            2601 or 2627 => DbErrorKind.DuplicateKey,
            547 => DbErrorKind.ForeignKeyViolation,
            515 => DbErrorKind.NotNullViolation,
            1205 => DbErrorKind.Deadlock,
            40501 or 4060 => DbErrorKind.SerializationFailureOrRetryable, // voorbeeld, pas evt. aan
            _ => DbErrorKind.Unknown
        };

        private static int? GetSqlErrorNumber(object sqlEx)
            => GetIntProperty(sqlEx, "Number");

        private static int? GetFromErrorsCollection(object sqlEx)
        {
            var errors = sqlEx.GetType().GetProperty("Errors", BindingFlags.Public | BindingFlags.Instance)
                              ?.GetValue(sqlEx);
            if (errors is null) return null;

            var countObj = errors.GetType().GetProperty("Count")?.GetValue(errors);
            var getItem = errors.GetType().GetMethod("get_Item", new[] { typeof(int) });
            if (countObj is null || getItem is null) return null;

            if (Convert.ToInt32(countObj) <= 0) return null;

            var firstError = getItem.Invoke(errors, new object[] { 0 });
            return GetIntProperty(firstError!, "Number");
        }

        private static int? GetIntProperty(object instance, string propertyName)
        {
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var val = prop?.GetValue(instance);
            if (val is null) return null;
            try { return Convert.ToInt32(val); } catch { return null; }
        }

        private static Guid? GetGuidProperty(object instance, string propertyName)
        {
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            var val = prop?.GetValue(instance);
            return val is Guid g ? g : (Guid?)null;
        }
    }
}
