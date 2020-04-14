using System;

namespace CQELight.EventStore.EFCore
{
    /// <summary>
    /// Buffer information for EF Core event store.
    /// </summary>
    public class BufferInfo
    {
        #region Static properties

        /// <summary>
        /// Default activated buffer infos.
        /// </summary>
        public static BufferInfo Default
            => new BufferInfo(true);
        /// <summary>
        /// Default deactivated buffer infos.
        /// </summary>
        public static BufferInfo Disabled
            => new BufferInfo(false);

        #endregion

        #region Properties

        /// <summary>
        /// Flag that indicates if a buffer should be used.
        /// If true, a buffer will be used to store events, according
        /// to defined TimeOuts to avoid making to many single
        /// request to database.
        /// </summary>
        public bool UseBuffer { get; private set; }
        /// <summary>
        /// Absolute timeout of buffer usage.
        /// Starting first event, after this periode, buffer will be saved no matter what.
        /// </summary>
        public TimeSpan AbsoluteTimeOut { get; private set; }
        /// <summary>
        /// Sliding timeout of buffer usage.
        /// Starting first event, counter begins. If next event comes before
        /// counter reach slidingTimeout, counter is reset, else, event(s) are
        /// persisted.
        /// </summary>
        public TimeSpan SlidingTimeOut { get; private set; }

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new buffer info that use
        /// two specific TimeSpan. Using buffer might not be
        /// appropriate if fresh events are needed as soon as they're
        /// dispatched.
        /// </summary>
        /// <param name="absoluteTimeOut">Absolute timeout.</param>
        /// <param name="slidingTimeOut">Sliding timeout.</param>
        public BufferInfo(TimeSpan absoluteTimeOut, TimeSpan slidingTimeOut)
        {
            UseBuffer = true;
            AbsoluteTimeOut = absoluteTimeOut;
            SlidingTimeOut = slidingTimeOut;
        }

        /// <summary>
        /// Creates a new buffer info.
        /// If true is passed, it uses default values
        /// (10 sec absolute timeout, 2 sec sliding timeout).
        /// If you want to specify custome timeout, use
        /// other constructor to specify both values.
        /// </summary>
        /// <param name="useBuffer">Use buffer or not.</param>
        public BufferInfo(bool useBuffer)
        {
            UseBuffer = useBuffer;
            if (useBuffer)
            {
                AbsoluteTimeOut = new TimeSpan(0, 0, 10);
                SlidingTimeOut = new TimeSpan(0, 0, 2);
            }
        }

        #endregion
    }
}
