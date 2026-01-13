using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    /// <summary>
    /// Probeert handlers op rij (Chain of Responsibility / Composite).
    /// </summary>
    public sealed class RelationalSaveChangesDbUpdateExceptionHandler : ISaveChangesDbUpdateExceptionHandler
    {
        private readonly IReadOnlyList<ISaveChangesDbUpdateExceptionHandler> _handlers;

        public RelationalSaveChangesDbUpdateExceptionHandler(IEnumerable<ISaveChangesDbUpdateExceptionHandler> handlers)
            => _handlers = handlers.ToList();

        public bool TryParse(DbUpdateException ex, out DbExceptionInfo info)
        {
            foreach (var h in _handlers)
            {
                if (h.TryParse(ex, out info))
                    return true;
            }
            info = new DbExceptionInfo { OriginalException = ex };
            return false;
        }
    }
}
