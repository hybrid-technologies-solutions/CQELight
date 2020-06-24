using PizzaApp.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace PizzaApp.Converters
{
    public class SortTypeToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is SortType sort)
            {
                return sort switch
                {
                    SortType.Name => "sort_name.png",
                    SortType.Price => "sort_price.png",
                    SortType.Favorite => "sort_fav.png",
                    _ => "sort_none.png"
                };
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
