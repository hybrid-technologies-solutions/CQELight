using CQELight.MVVM.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CQELight.MVVM
{
    /// <summary>
    /// Base class for view model that can handle errors.
    /// </summary>
    public abstract class BaseErrorViewModel : BaseViewModel, INotifyDataErrorInfo
    {
        #region Members

        private readonly IDictionary<string, IList<string>> _errors
            = new Dictionary<string, IList<string>>();

        #endregion

        #region Ctor

        protected BaseErrorViewModel()
            : base()
        {
        }

        protected BaseErrorViewModel(IView holder)
            : base(holder)
        {
        }

        #endregion

        #region INotifyDataErrorInfo

        /// <summary>
        /// Gets the info if there's any informations.
        /// </summary>
        public bool HasErrors => _errors.Count > 0;
        /// <summary>
        /// Event to fire when error collection are changed.
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
        /// <summary>
        /// Get errors based on property names.
        /// </summary>
        /// <param name="propertyName">Property name on which we wnat errors..</param>
        /// <returns>All errors for this property name, or empty collection, or null if none.</returns>
        public IEnumerable? GetErrors(string propertyName)
        {
            if (!string.IsNullOrWhiteSpace(propertyName))
            {
                return _errors.ContainsKey(propertyName) ? _errors[propertyName] : null;
            }
            return null;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Add an error in the collection.
        /// </summary>
        /// <param name="error">Error info to add to collection.</param>
        /// <param name="propertyName">Name of the property. Can be retrieve by the calling member.</param>
        protected void AddError(string error, [CallerMemberName]string propertyName = "")
        {
            if (!_errors.ContainsKey(propertyName))
            {
                _errors[propertyName] = new List<string>();
            }
            if (!_errors[propertyName].Contains(error))
            {
                _errors[propertyName].Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Clear all errors for a specific property name.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void ClearErrors([CallerMemberName]string propertyName = "")
        {
            if (_errors.ContainsKey(propertyName))
            {
                _errors.Remove(propertyName);
                OnErrorsChanged(propertyName);
            }
        }

        /// <summary>
        /// Method to notify listeners that error are changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected virtual void OnErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            RaisePropertyChanged(nameof(HasErrors));
        }

        #endregion
    }
}
