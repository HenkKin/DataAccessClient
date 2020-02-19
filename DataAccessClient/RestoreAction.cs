using System;

namespace DataAccessClient
{
    public class RestoreAction : IDisposable
    {
        private Func<bool> _dispose;

        public RestoreAction(Func<bool> dispose)
        {
            _dispose = dispose;
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
                if (_dispose == null)
                {
                    return;
                }

                try
                {
                    _dispose();
                }
                catch
                {
                    // ignored
                }

                _dispose = null;
            }
        }
    }
}