using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PizzaApp
{
    public class StorageKeys
    {
        public const string Sort_Key = "KEY_Sort";
        public const string Pizza_Favorite_Key = "KEY_Pizza_{0}_Fav";
    }

    public class Filenames
    {
        public static string Pizzas_FileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "pizzas.json");
    }
}
