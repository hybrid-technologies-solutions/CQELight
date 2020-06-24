using System;
using System.Collections.Generic;
using System.Text;

namespace PizzaApp.Models
{
    public class Pizza
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public IEnumerable<string> Ingredients { get; set; }
        public string ImageUrl { get; set; }
        public bool IsFavorite { get; set; }
    }
}
