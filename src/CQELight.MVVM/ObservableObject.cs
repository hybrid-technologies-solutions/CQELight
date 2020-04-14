using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CQELight.MVVM
{
    /// <summary>
    /// Base class to implement INotifyPropertyChanged event.
    /// </summary>
    public class ObservableObject : INotifyPropertyChanged
    {
        #region Events

        /// <summary>
        /// Event to notify of change
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        #endregion

        #region Protected methods

        /// <summary>
        /// Set a new value to a member.
        /// </summary>
        /// <typeparam name="T">Type of member to set.</typeparam>
        /// <param name="member">Reference to member.</param>
        /// <param name="value">New value.</param>
        /// <param name="memberName">Name of the property.</param>
        protected bool Set<T>(ref T member, T value, [CallerMemberName] string memberName = "")
        {
            if (EqualityComparer<T>.Default.Equals(member, value))
                return false;

            member = value;
            RaisePropertyChanged(memberName);
            return true;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Raise the event propertyChanged to all subscribers.
        /// </summary>
        /// <param name="prop">Name of proprety to raise.</param>
        public void RaisePropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

    }
}
