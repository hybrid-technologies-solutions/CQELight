using CQELight.MVVM;
using PizzaApp.Models;
using PizzaApp.Storage;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using Xamarin.Forms;
using static PizzaApp.StorageKeys;

namespace PizzaApp.ViewModels
{
    public class PizzaListViewCell : ObservableObject
    {
        private Pizza pizza;

        public bool IsFavorite
        {
            get => pizza.IsFavorite;
            set { pizza.IsFavorite = value; RaisePropertyChanged(); }
        }

        public int Id => pizza.Id;

        public string ImageUrl => pizza.ImageUrl;

        public string Name => pizza.Name;

        public IEnumerable<string> Ingredients => pizza.Ingredients;

        public int Price => pizza.Price;

        public ICommand MarkAsFavoriteCommand { get; set; }

        public PizzaListViewCell(Pizza pizza, IAppStorage appStorage)
        {
            this.pizza = pizza;

            MarkAsFavoriteCommand = new Command(id =>
            {
                IsFavorite = !IsFavorite;
                appStorage.StoreValueAsync(string.Format(Pizza_Favorite_Key, id), pizza.IsFavorite);
            });
        }
    }
}
