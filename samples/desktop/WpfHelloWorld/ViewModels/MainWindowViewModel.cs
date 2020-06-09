using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Dispatcher.Interfaces;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher;
using CQELight.MVVM;
using CQELight.MVVM.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfHelloWorld.Commands;
using WpfHelloWorld.Events;

namespace WpfHelloWorld.ViewModels
{
    public class MainWindowViewModel
        : BaseViewModel,
        IDomainEventHandler<HelloSaidEvent>,
        IAutoRegisterType
    {
        public ICommand SayHelloCommand { get; set; }

        public MainWindowViewModel(IView view, IDispatcher dispatcher)
            : base(view)
        {
            SayHelloCommand = new AsyncDelegateCommand(_ => dispatcher.DispatchCommandAsync(new SayHelloCommand()));
        }

        public async Task<Result> HandleAsync(HelloSaidEvent domainEvent, IEventContext context = null)
        {
            await _view.ShowAlertAsync("Hello !", "Hello World!");
            return Result.Ok();
        }
    }
}
