using CQELight.Dispatcher;
using CQELight.MVVM.Common;
using CQELight.MVVM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CQELight.MVVM.XamarinForms
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContentPageCQELight : ContentPage, IView
    {
        private StackLayout? LoadingPanel { get; set; }

        private ContentPresenter? MainContent { get; set; }

        private Label? WaitingLabel { get; set; }

        public ContentPageCQELight()
        {
            InitializeComponent();
            CoreDispatcher.AddHandlerToDispatcher(this);
        }

        protected override async void OnAppearing()
        {
            if (BindingContext is BaseViewModel bvm)
            {
                await bvm.OnLoadCompleteAsync();
            }
            base.OnAppearing();
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            LoadingPanel = (StackLayout)GetTemplateChild("pnl_waiting");
            WaitingLabel = (Label)GetTemplateChild("lbl_waiting");
            MainContent = (ContentPresenter)GetTemplateChild("pnl_main");
        }

        public void Close()
        {
            if (Navigation != null)
            {
                Navigation.PopAsync();
                return;
            }
            throw new InvalidOperationException("In order to call 'Close' on a ContentPage, it should be located within a NavigationPage");
        }

        public Task HideLoadingPanelAsync()
        {
            if (LoadingPanel == null || MainContent == null)
            {
                throw new InvalidOperationException($"Unable to call '{nameof(HideLoadingPanelAsync)}' because template elements 'LoadingPanel' or 'MainContent' weren't found. Ensure that you didn't overwrite ContentTemplate in your page.");
            }
            return Device.InvokeOnMainThreadAsync(() =>
            {
                LoadingPanel.IsVisible = false;
                MainContent.IsVisible = true;
            });
        }

        public void HideView() => IsVisible = false;

        public void PerformOnUIThread(Action act)
        {
            Device.InvokeOnMainThreadAsync(act);
        }

        public async Task ShowAlertAsync(string title, string message, MessageDialogServiceOptions? options = null)
        {
            if (options?.ShowCancel == true)
            {
                var result = await DisplayAlert(Title, message, "OK", "Cancel");
                if (!result)
                {
                    options.CancelCallback?.Invoke();
                }
            }
            else
            {
                await DisplayAlert(Title, message, "OK");
            }
        }

        public Task ShowLoadingPanelAsync(string waitMessage, LoadingPanelOptions? options = null)
        {
            if (LoadingPanel == null || MainContent == null || WaitingLabel == null)
            {
                throw new InvalidOperationException($"Unable to call '{nameof(HideLoadingPanelAsync)}' because template elements 'LoadingPanel', 'MainContent' or 'WaitingLabel' weren't found. Ensure that you didn't overwrite ContentTemplate in your page.");
            }
            return Device.InvokeOnMainThreadAsync(() =>
            {
                MainContent.IsVisible = false;
                WaitingLabel.Text = waitMessage;
                LoadingPanel.IsVisible = true;
            });
        }

        public void ShowPopup(IView popupWindow)
        {
            if (Navigation != null)
            {
                if (popupWindow is Page popupPage)
                {
                    Navigation.PushModalAsync(popupPage);
                    return;
                }
                else
                {
                    throw new InvalidOperationException("Popup passed doesn't inherits from 'Xamarin.Forms.Page', so it can't be used to display popup");
                }
            }
            throw new InvalidOperationException("In order to call 'ShowPopup' on a ContentPage, it should be located within a NavigationPage");
        }

        public void ShowView() => IsVisible = true;

        public async Task<bool> ShowYesNoDialogAsync(string title, string message, MessageDialogServiceOptions? options = null)
        {
            var result = await DisplayAlert(Title, message, "Yes", "No");
            if (!result)
            {
                options?.CancelCallback?.Invoke();
            }
            return result;
        }
    }
}