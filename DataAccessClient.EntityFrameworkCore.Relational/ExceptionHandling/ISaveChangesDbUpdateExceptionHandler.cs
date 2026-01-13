using Microsoft.EntityFrameworkCore;

namespace DataAccessClient.EntityFrameworkCore.Relational.ExceptionHandling
{
    public interface ISaveChangesDbUpdateExceptionHandler
    {
        /// <summary>
        /// Probeert de gegeven exception te herkennen en te mappen.
        /// Retourneert true als deze handler de exception herkent en vult DbExceptionInfo.
        /// </summary>
        bool TryParse(DbUpdateException ex, out DbExceptionInfo info);
    }
}
