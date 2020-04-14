using CQELight.Dispatcher;
using CQELight.MVVM.Common;
using CQELight.MVVM.Interfaces;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CQELight.MVVM.MahApps
{
    /// <summary>
    /// Implementation of MahApps.MetroWindow to be fully compatible with
    /// CQELight MVVM implementation.
    /// </summary>
    public class CQEMetroWindow : MetroWindow, IView
    {
        #region Members

        private ProgressDialogController? _progressAwaiter;
        private CancellationTokenSource? _showLoadingCancel;

        #endregion

        #region Ctor

        /// <summary>
        /// Creates a new instance of <see cref="CQEMetroWindow"/>
        /// </summary>
        public CQEMetroWindow()
        {
            Loaded += async (s, e) =>
            {
                if (DataContext is BaseViewModel viewModel)
                {
                    await viewModel.OnLoadCompleteAsync();
                }
            };
            Closing += async (s, e) =>
            {
                if (DataContext is BaseViewModel bvm)
                {
                    var handled = await bvm.OnCloseAsync();
                    if (!handled)
                    {
                        e.Cancel = true;
                    }
                }
            };
            CoreDispatcher.AddHandlerToDispatcher(this);
        }

        #endregion

        #region IView methods

        public async Task HideLoadingPanelAsync()
        {
            if (_progressAwaiter != null)
            {
                await Application.Current.Dispatcher.Invoke(async () => await _progressAwaiter.CloseAsync().ConfigureAwait(false)).ConfigureAwait(false);
                try
                {
                    if (_showLoadingCancel?.IsCancellationRequested == false)
                    {
                        _showLoadingCancel.Cancel();
                    }
                }
                catch
                {
                    //Failing cancel should not throw
                }
            }
        }

        public void HideView()
            => Application.Current.Dispatcher.Invoke(Hide);

        public void PerformOnUIThread(Action act)
        {
            if (act is null)
            {
                throw new ArgumentNullException(nameof(act));
            }
            Application.Current.Dispatcher.Invoke(act);
        }

        public Task ShowAlertAsync(string title, string message, MessageDialogServiceOptions? options = null)
            => Application.Current.Dispatcher.Invoke(async () =>
                {
                    await this.ShowMessageAsync(title, message, MessageDialogStyle.Affirmative, new MetroDialogSettings
                    {
                        AffirmativeButtonText = "Ok"
                    }).ConfigureAwait(false);
                });

        public Task ShowLoadingPanelAsync(string waitMessage, LoadingPanelOptions? options = null)
        {
            Application.Current.Dispatcher.Invoke(async () =>
            {
                _progressAwaiter = await this.ShowProgressAsync("Please wait...", waitMessage).ConfigureAwait(false);
                if (options?.Timeout > 0)
                {
                    try
                    {
                        _showLoadingCancel = new CancellationTokenSource();
#pragma warning disable CS4014
                        Task.Delay(Convert.ToInt32(options.Timeout), _showLoadingCancel.Token).ContinueWith(async a =>
                        {
                            if (!a.IsCanceled && !string.IsNullOrWhiteSpace(options.TimeoutErrorMessage))
                            {
                                await ShowAlertAsync("Error", options.TimeoutErrorMessage, new MessageDialogServiceOptions
                                {
                                    DialogStyle = AlertType.Error
                                }).ConfigureAwait(false);
                            }
                        });
#pragma warning restore CS4014
                    }
                    catch
                    {
                        //Failing cancel should not throw
                    }
                }
            });
            return Task.CompletedTask;
        }

        public void ShowPopup(IView popupWindow)
        {
            if (popupWindow is Window baseW)
            {
                Application.Current.Invoke(() => baseW.ShowDialog());
            }
            else
            {
                throw new InvalidOperationException("Impossible to show popup that doesn't inherits from WPF base Window class.");
            }
        }

        public void ShowView()
            => Application.Current.Dispatcher.Invoke(Show);

        public async Task<bool> ShowYesNoDialogAsync(string title, string message, MessageDialogServiceOptions? options = null)
        {
            var settings = new MetroDialogSettings();
            if (options?.ShowCancel == true)
            {
                settings.FirstAuxiliaryButtonText = "Cancel";
            }
            bool result = false;
            await Application.Current.Dispatcher.Invoke(async () =>
            {
                var msgBoxResult = await this.ShowMessageAsync(title, message, MessageDialogStyle.AffirmativeAndNegative, null).ConfigureAwait(false);
                if (msgBoxResult == MessageDialogResult.Affirmative)
                {
                    result = true;
                }
                else if (msgBoxResult == MessageDialogResult.FirstAuxiliary && options?.ShowCancel == true && options.CancelCallback != null)
                {
                    options.CancelCallback();
                }
            }).ConfigureAwait(false);
            return result;
        }

        #endregion

    }
}
