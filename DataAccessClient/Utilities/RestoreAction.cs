using System;

namespace DataAccessClient.Utilities
{
    public class RestoreAction : IDisposable
    {
        private Action _dispose;

        public RestoreAction(Action dispose)
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