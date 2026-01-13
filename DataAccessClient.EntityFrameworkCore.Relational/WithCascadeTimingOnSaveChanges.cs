using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace DataAccessClient.EntityFrameworkCore.Relational
{
    internal class WithCascadeTimingOnSaveChanges : IDisposable
    {
        private DbContext _dbContext;
        private CascadeTiming _originalCascadeDeleteTiming;
        private CascadeTiming _originalDeleteOrphansTiming;

        public WithCascadeTimingOnSaveChanges(DbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Run(Action work)
        {
            _originalCascadeDeleteTiming = _dbContext.ChangeTracker.CascadeDeleteTiming;
            _originalDeleteOrphansTiming = _dbContext.ChangeTracker.DeleteOrphansTiming;

            _dbContext.ChangeTracker.CascadeDeleteTiming = CascadeTiming.OnSaveChanges;
            _dbContext.ChangeTracker.DeleteOrphansTiming = CascadeTiming.OnSaveChanges;
            work?.Invoke();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_dbContext == null)
                {
                    return;
                }

                try
                {
                    _dbContext.ChangeTracker.CascadeDeleteTiming = _originalCascadeDeleteTiming;
                    _dbContext.ChangeTracker.DeleteOrphansTiming = _originalDeleteOrphansTiming;
                }
                catch
                {
                    // ignored
                }

                _dbContext = null;
            }
        }
    }
}