using CQELight.Abstractions.CQS.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher;
using CQELight.MVVM.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
using System;
using System.Threading.Tasks;
using W = System.Windows.Input;

namespace CQELight.MVVM
{
    /// <summary>
    /// Definition class for view models. A view model is an observable object that can notify view for changes.
    /// It's also a IoC scope holder, a command context for dispatching, an event context for dispatching and
    /// is disposable.
    /// </summary>
    public abstract class BaseViewModel : ObservableObject, IScopeHolder, ICommandContext, IEventContext, IDisposable
    {
        #region Members

        protected IScope? _scope;
        protected IView? _view;
        protected ILogger _logger;

        #endregion

        #region Properties

        /// <summary>
        /// IoC Scope.
        /// </summary>
        public IScope? Scope => _scope;

        /// <summary>
        /// Command for cancel action.
        /// </summary>
        public W.ICommand CancelCommand
            => new DelegateCommand(_ => Cancel());

        #endregion

        #region Ctor

        protected BaseViewModel(IScopeFactory? scopeFactory = null)
        {
            if (scopeFactory != null)
            {
                _scope = scopeFactory.CreateScope();
            }
            _logger =
                _scope?.Resolve<ILoggerFactory>()?.CreateLogger(GetType().Name)
                ??
                new LoggerFactory(new[] { new DebugLoggerProvider() }).CreateLogger(GetType().Name);
            CoreDispatcher.AddHandlerToDispatcher(this);
        }

        protected BaseViewModel(IView view, IScopeFactory? scopeFactory = null)
            : this(scopeFactory)
        {
            _view = view;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Base method to cancel action.
        /// </summary>
        public virtual void Cancel()
            => _view?.Close();

        /// <summary>
        /// Action to perform when view is loaded.
        /// </summary>
        public virtual Task OnLoadCompleteAsync()
        {
            return Task.FromResult(0);
        }

        /// <summary>
        /// Action to perform when view is closing.
        /// </summary>
        /// <returns>If true is returned, view can closed. If false is returned, closing is canceled.</returns>
        public virtual Task<bool> OnCloseAsync()
        {
            return Task.FromResult(true);
        }

        /// <summary>
        /// Cleaning up viewmodel resources.
        /// </summary>
        public virtual void Dispose()
        {
        }

        #endregion

    }
}
