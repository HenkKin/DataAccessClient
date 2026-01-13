using Microsoft.EntityFrameworkCore;
using System;
using System.Reflection;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling.Providers
{

    public sealed class PostgresExceptionHandler : ISaveChangesDbUpdateExceptionHandler
    {
        public bool TryParse(DbUpdateException ex, out DbExceptionInfo info)
        {
            info = default!;
            var pgEx = FindPostgresException(ex);
            if (pgEx is null) return false;

            var sqlState = GetStringProperty(pgEx, "SqlState");
            var constraint = GetStringProperty(pgEx, "ConstraintName");
            var table = GetStringProperty(pgEx, "TableName");
            var schema = GetStringProperty(pgEx, "SchemaName");

            var kind = ClassifyBySqlState(sqlState);

            info = new DbExceptionInfo
            {
                Kind = kind,
                Message = pgEx.Message,
                Provider = "PostgreSql",
                SqlState = sqlState,
                Constraint = constraint,
                Table = table,
                Schema = schema,
                OriginalException = ex
            };
            return true;
        }

        private static Exception? FindPostgresException(DbUpdateException ex)
        {
            foreach (var e in ExceptionChain.Traverse(ex))
            {
                var t = e.GetType();
                if (t.Namespace == "Npgsql" && t.Name == "PostgresException")
                    return e;
            }
            // desnoods val terug op NpgsqlException (minder detail)
            foreach (var e in ExceptionChain.Traverse(ex))
            {
                var t = e.GetType();
                if (t.Namespace == "Npgsql" && t.Name == "NpgsqlException")
                    return e;
            }
            return null;
        }

        private static DbErrorKind ClassifyBySqlState(string? sqlState) => sqlState switch
        {
            "23505" => DbErrorKind.DuplicateKey,
            "23503" => DbErrorKind.ForeignKeyViolation,
            "23502" => DbErrorKind.NotNullViolation,
            "23514" => DbErrorKind.CheckViolation,
            "40P01" => DbErrorKind.Deadlock,
            "40001" or "55P03" => DbErrorKind.SerializationFailureOrRetryable, // retrybaar
            _ => DbErrorKind.Unknown
        };

        private static string? GetStringProperty(object instance, string propertyName)
        {
            var prop = instance.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            return prop?.GetValue(instance) as string;
        }
    }
}
