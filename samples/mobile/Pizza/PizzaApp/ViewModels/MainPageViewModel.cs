using CQELight.Abstractions.DDD;
using CQELight.Abstractions.Events.Interfaces;
using CQELight.Abstractions.IoC.Interfaces;
using CQELight.Dispatcher;
using CQELight.MVVM;
using CQELight.MVVM.Interfaces;
using CQELight.Tools.Extensions;
using Newtonsoft.Json;
using PizzaApp.Commands;
using PizzaApp.Events;
using PizzaApp.Models;
using PizzaApp.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Forms;
using static PizzaApp.StorageKeys;

namespace PizzaApp.ViewModels
{
    public class MainPageViewModel :
        BaseViewModel,
        IDomainEventHandler<PizzasLoaded>,
        IDomainEventHandler<PizzasNotLoaded>,
        IAutoRegisterType
    {
        private List<Pizza> pizzas;
        private readonly CQELight.Abstractions.Dispatcher.Interfaces.IDispatcher dispatcher;
        private readonly IAppStorage appStorage;

        private bool listVisible;
        public bool ListVisible
        {
            get => listVisible;
            set { listVisible = value; RaisePropertyChanged(); }
        }

        private bool listRefreshing;
        public bool ListRefreshing
        {
            get => listRefreshing;
            set { listRefreshing = value; RaisePropertyChanged(); }
        }

        private SortType sortType;
        private readonly IPizzaStorage pizzaStorage;

        public SortType SortType
        {
            get => sortType;
            set { sortType = value; RaisePropertyChanged(); OrderPizzas(pizzas); }
        }

        public ICommand RefreshPizzasCommand { get; set; }
        public ICommand ChangeSortCommand { get; set; }
        public ObservableCollection<PizzaListViewCell> Pizzas { get; set; }

        public MainPageViewModel(
            IView view,
            CQELight.Abstractions.Dispatcher.Interfaces.IDispatcher dispatcher,
            IAppStorage appStorage,
            IPizzaStorage pizzaStorage)
            : base(view)
        {
            Pizzas = new ObservableCollection<PizzaListViewCell>();
            RefreshPizzasCommand = new Command(() =>
            {
                _view.ShowLoadingPanelAsync("Chargement du menu ...");
                dispatcher.DispatchCommandAsync(new LoadPizza());
            });
            ChangeSortCommand = new Command(() =>
            {
                if (SortType == SortType.None)
                {
                    SortType = SortType.Name;
                }
                else if (SortType == SortType.Name)
                {
                    SortType = SortType.Price;
                }
                else if (SortType == SortType.Price)
                {
                    SortType = SortType.Favorite;
                }
                else
                {
                    SortType = SortType.None;
                }
                appStorage.StoreValueAsync(Sort_Key, (int)SortType);
            });
            this.dispatcher = dispatcher;
            this.appStorage = appStorage;

            var savedSort = appStorage.GetValue<int?>(Sort_Key);
            if (savedSort.HasValue)
            {
                SortType = (SortType)savedSort.Value;
            }

            this.pizzaStorage = pizzaStorage;
        }

        public override async Task OnLoadCompleteAsync()
        {
            await _view.ShowLoadingPanelAsync("Chargement du menu ...");
            await dispatcher.DispatchCommandAsync(new LoadPizza());
        }

        public async Task<Result> HandleAsync(PizzasLoaded domainEvent, IEventContext context = null)
        {
            await _view.HideLoadingPanelAsync();
            try
            {
                DisplayPizzasList(domainEvent.Pizzas.ToList());
            }
            catch
            {
                await _view.ShowAlertAsync("Erreur", "La réponse du serveur n'est pas au format attendu. Merci de relancer l'application.");
                _view.PerformOnUIThread(() =>
                {
                    ListVisible = true;
                });
            }

            return Result.Ok();
        }

        public async Task<Result> HandleAsync(PizzasNotLoaded domainEvent, IEventContext context = null)
        {
            await _view.HideLoadingPanelAsync();
            var pizzasFromDisk = await pizzaStorage.GetPizzasAsync();
            if (pizzasFromDisk.Any())
            {
                await _view.ShowAlertAsync("Erreur", "Impossible de récupérer le menu sur Internet. Les pizzas affichées sont celles sauvegardées lors de votre dernière visite");
                DisplayPizzasList(pizzasFromDisk.ToList());
            }
            else
            {
                await _view.ShowAlertAsync("Erreur", "Impossible de récupérer le menu sur Internet. Merci de vérifier votre connexion réseau et de réessayer.");
            }
            _view.PerformOnUIThread(() =>
            {
                ListVisible = true;
            });
            return Result.Ok();
        }

        private void DisplayPizzasList(List<Pizza> pizzaList)
        {
            _view.PerformOnUIThread(() =>
            {
                pizzaList.DoForEach(p => p.IsFavorite = appStorage.GetValue<bool>(string.Format(Pizza_Favorite_Key, p.Id)));
                pizzas = pizzaList;

                OrderPizzas(pizzas);

                ListVisible = true;
                ListRefreshing = false;
            });
        }

        private void OrderPizzas(IEnumerable<Pizza> pizzas)
        {
            if (pizzas?.Any() == true)
            {
                void AddPizzaToCollection(Pizza p) => Pizzas.Add(new PizzaListViewCell(p, appStorage));
                Pizzas.Clear();
                switch (SortType)
                {
                    case SortType.Name:
                        pizzas.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).DoForEach(AddPizzaToCollection);
                        break;
                    case SortType.Price:
                        pizzas.OrderBy(p => p.Price).DoForEach(AddPizzaToCollection);
                        break;
                    case SortType.Favorite:
                        pizzas.OrderBy(p => !p.IsFavorite).DoForEach(AddPizzaToCollection);
                        break;
                    default:
                        pizzas.DoForEach(AddPizzaToCollection);
                        break;
                }
            }
        }

    }
}
