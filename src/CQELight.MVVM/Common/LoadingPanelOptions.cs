namespace CQELight.MVVM.Common
{
    /// <summary>
    /// Options for loading panel displayer.
    /// </summary>
    public class LoadingPanelOptions
    {
        #region Properties

        /// <summary>
        /// Timeout in milliseconds.
        /// </summary>
        public uint Timeout { get; set; }
        /// <summary>
        /// An error message
        /// </summary>
        public string TimeoutErrorMessage { get; set; } = "";

        #endregion

    }
}
