using Microsoft.AspNetCore.Mvc.ViewEngines;
using System.ComponentModel.DataAnnotations;

namespace Restaurants.Models
{
    public class Restaurant
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Menu type is required")]
        public string MenuType { get; set; }
        [Required]
        public Address Address { get; set; }
        public List<Review> Reviews { get; set; } = new();
    }
}
