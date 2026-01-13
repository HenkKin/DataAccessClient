using System;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    public sealed class DbExceptionInfo
    {
        public DbErrorKind Kind { get; init; } = DbErrorKind.Unknown;
        public string? Message { get; init; }
        public string? Provider { get; init; }         // "SqlServer" / "PostgreSql" / ...
        public int? Number { get; init; }              // SQL Server
        public string? SqlState { get; init; }         // PostgreSQL
        public string? Constraint { get; init; }       // PG: ConstraintName
        public string? Table { get; init; }            // PG: TableName
        public string? Schema { get; init; }           // PG: SchemaName
        public Guid? ClientConnectionId { get; init; } // SQL Server
        public Exception OriginalException { get; init; } = new Exception();
    }
}
