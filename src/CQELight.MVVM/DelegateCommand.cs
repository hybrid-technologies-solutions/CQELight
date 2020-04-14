using System;

namespace CQELight.MVVM
{
    /// <summary>
    /// Simple delegate command.
    /// </summary>
    public class DelegateCommand : System.Windows.Input.ICommand
    {
        #region Members

        /// <summary>
        /// Test to execute to see if command is runnable.
        /// </summary>
        private readonly Predicate<object>? canExecute;
        /// <summary>
        /// Associated action.
        /// </summary>
        private readonly Action<object> execute;

        /// <summary>
        /// Handler to notify CommandManagers that CanExecute predicate has changed.
        /// </summary>
        public virtual event EventHandler? CanExecuteChanged;

        #endregion 

        #region Ctor

        /// <summary>
        /// Create a new delegate command
        /// </summary>
        /// <param name="execute">Command to execute.</param>
        /// <param name="canExecute">Test to verify if command is runnable.</param>
        public DelegateCommand(Action<object> execute, Predicate<object>? canExecute = null)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Raise CanExecuteChanged to all listeners.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region ICommand methods

        /// <summary>
        /// Evaluate the predicate to see if command is runnable.
        /// </summary>
        /// <param name="parameter">Parameter.</param>
        /// <returns>True if the predicate returns true or doesn't exists, false otherwise.</returns>
        public bool CanExecute(object parameter)
        {
            return canExecute == null || canExecute(parameter);
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        /// <param name="parameter">Parameter.</param>
        public void Execute(object parameter)
        {
            execute?.Invoke(parameter);
        }

        #endregion
    }
}
