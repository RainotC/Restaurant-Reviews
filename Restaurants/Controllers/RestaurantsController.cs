using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Restaurants.Data;
using Restaurants.Models;
using Restaurants.Services;

namespace Restaurants.Controllers
{
    public class RestaurantsController : Controller
    {
        private readonly AppDbContext _context;
		private readonly GeolocationService _geoService;
		public RestaurantsController(AppDbContext context, GeolocationService geoService)
        {
			_geoService = geoService;
			_context = context;
        }

        // GET: Restaurants
        public async Task<IActionResult> Index(string sortOrder, string searchString, string menuFilter)
        {
            ViewData["NameSortParm"] = String.IsNullOrEmpty(sortOrder) ? "name_desc" : "";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentMenu"] = menuFilter;

            var restaurants = from r in _context.Restaurants.Include(r => r.Reviews)
                              select r;

            if (!String.IsNullOrEmpty(searchString))
            {
                restaurants = restaurants.Where(r => r.Name.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(menuFilter))
            {
                restaurants = restaurants.Where(r => r.MenuType == menuFilter);
            }

            restaurants = sortOrder switch
            {
                "name_desc" => restaurants.OrderByDescending(r => r.Name),
                _ => restaurants.OrderBy(r => r.Name),
            };

            var menuTypes = await _context.Restaurants
             .Select(r => r.MenuType)
             .Distinct()
             .OrderBy(m => m)
             .ToListAsync();
            ViewBag.MenuTypes = new SelectList(menuTypes);

            var list = await restaurants.ToListAsync();
            var averageRatings = list.ToDictionary(
                r => r.Id,
                r => r.Reviews.Any() ? r.Reviews.Average(rv => rv.Rating) : 0);
            ViewData["AverageRatings"] = averageRatings;

            return View(list);
        }


        // GET: Restaurants/Nearby
        [HttpGet]
        public async Task<IActionResult> Nearby(double lat, double lon)
		{
			var allRestaurants = await _context.Restaurants.Include(r => r.Address).ToListAsync();

			var userLocation = new GeoLocation { Latitude = lat, Longitude = lon };

			var nearbyRestaurants = allRestaurants.Where(r =>
			{
				
				var addressString = $"{r.Address.Street}, {r.Address.ZipCode} {r.Address.City}";
				var coords = _geoService.GetCoordinatesAsync(addressString).GetAwaiter().GetResult();

				if (coords == null) return false;

				var restaurantLocation = new GeoLocation { Latitude = coords.Value.lat, Longitude = coords.Value.lon };
				var distance = _geoService.CalculateDistance(userLocation, restaurantLocation);
                 
                return distance <= 10; // 10 km radius
            }).ToList();

			return View("Index", nearbyRestaurants); 
		}


	// GET: Restaurants/Details/5
	public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .Include(r => r.Address)
                .Include(r => r.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }
            ViewData["AverageRating"] = restaurant.Reviews.Any()
                ? restaurant.Reviews.Average(r => r.Rating)
                : 0;

            return View(restaurant);
        }

        private async Task SetCoordinatesAsync(Restaurant restaurant)
        {
            var addressString = $"{restaurant.Address.Street}, {restaurant.Address.City}, {restaurant.Address.ZipCode}";
            var coords = await _geoService.GetCoordinatesAsync(addressString);
            if (coords != null)
            {
                restaurant.Latitude = coords.Value.lat;
                restaurant.Longitude = coords.Value.lon;
            }
        }


        // GET: Restaurants/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Restaurants/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Restaurant restaurant)
        {
            if (ModelState.IsValid)
            {
                await SetCoordinatesAsync(restaurant);

                _context.Add(restaurant);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant == null)
            {
                return NotFound();
            }
            return View(restaurant);
        }

        // POST: Restaurants/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Restaurant restaurant)
        {
            if (id != restaurant.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await SetCoordinatesAsync(restaurant);
                    _context.Update(restaurant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RestaurantExists(restaurant.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(restaurant);
        }

        // GET: Restaurants/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var restaurant = await _context.Restaurants
                .FirstOrDefaultAsync(m => m.Id == id);
            if (restaurant == null)
            {
                return NotFound();
            }

            return View(restaurant);
        }

        // POST: Restaurants/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var restaurant = await _context.Restaurants.FindAsync(id);
            if (restaurant != null)
            {
                _context.Restaurants.Remove(restaurant);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // GET: Restaurants/AddReview/5
        public IActionResult AddReview(int id)  // id restauracji
        {
            var review = new Review { RestaurantId = id };
            return View(review);
        }

        // POST: Restaurants/AddReview
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(Review review)
        {
            if (ModelState.IsValid)
            {
                review.Id = 0;
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Details), new { id = review.RestaurantId });
            }

            return View(review);
        }


        private bool RestaurantExists(int id)
        {
            return _context.Restaurants.Any(e => e.Id == id);
        }
    }
}
