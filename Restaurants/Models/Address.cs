using System.ComponentModel.DataAnnotations;

namespace Restaurants.Models
{
    public class Address
    {
        [Required(ErrorMessage = "Street is required")]
        public required string Street { get; set; }
        [Required(ErrorMessage = "City is required")]
        public required string City { get; set; }
        [Required(ErrorMessage = "Zip code is required")]
        [RegularExpression(@"\d{2}-\d{3}", ErrorMessage = "Zip code needs to be in XX-XXX format")]
        public required string ZipCode { get; set; }
    }
}
