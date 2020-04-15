using System;

namespace CQELight.Tools
{
    /// <summary>
    /// Base class for implementing IDisposable behavior.
    /// </summary>
    public abstract class DisposableObject : IDisposable
    {
        #region Members

        /// <summary>
        /// Flag to indicate if resources have been clean or not.
        /// </summary>
        protected bool _disposed;

        #endregion

        #region Ctor & dtor

        /// <summary>
        /// Base ctor.
        /// </summary>
        protected DisposableObject()
        {
        }

        /// <summary>
        /// Finalize.
        /// </summary>
        ~DisposableObject() => Dispose(false);

        #endregion

        #region IDisposable pattern

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() => Dispose(true);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">Flag that indicates if comming from Dispose method of finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                GC.SuppressFinalize(this);
            }
            if(_disposed)
            {
                return;
            }
            _disposed = true;
        }

        #endregion
    }
}
