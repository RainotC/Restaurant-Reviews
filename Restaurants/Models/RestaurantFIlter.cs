using Microsoft.AspNetCore.Mvc.Rendering;

namespace Restaurants.Models
{
    public class RestaurantFIlter
    {
        public string SelectedMenuType { get; set; }
        public List<SelectListItem> MenuTypes { get; set; }

        public string SelectedCity { get; set; }
        public List<SelectListItem> Cities { get; set; }

        public List<Restaurant> Restaurants { get; set; }
    }
}
