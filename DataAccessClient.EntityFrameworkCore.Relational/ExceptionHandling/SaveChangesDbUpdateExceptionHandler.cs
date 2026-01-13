using DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling.Providers;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    public static class SaveChangesDbUpdateExceptionHandler
    {
        private static readonly ISaveChangesDbUpdateExceptionHandler _exceptionHandler = new RelationalSaveChangesDbUpdateExceptionHandler(
            new List<ISaveChangesDbUpdateExceptionHandler> {
                new SqlServerExceptionHandler(),
                new PostgresExceptionHandler()
            });

        public static DbErrorKind Handle(DbUpdateException ex, out DbExceptionInfo? info)
        {
            if (_exceptionHandler.TryParse(ex, out var parsed))
            {
                info = parsed;
                return parsed.Kind;
            }
            info = null;
            return DbErrorKind.Unknown;
        }
    }
}
