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
        public async Task<IActionResult> Index(string sortOrder, string searchString, string menuFilter, double? lat, double? lon)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["NameSortParm"] = sortOrder == "name" ? "name_desc" : "name";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentMenu"] = menuFilter;

            ViewData["UserLat"] = lat?.ToString() ?? "";
            ViewData["UserLon"] = lon?.ToString() ?? "";

            var restaurants = _context.Restaurants
                                      .Include(r => r.Reviews)
                                      .Include(r => r.Address)
                                      .AsQueryable();

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

            var list = await restaurants.ToListAsync();

            
            if (lat.HasValue && lon.HasValue)
            {
                var userLocation = new GeoLocation { Latitude = lat.Value, Longitude = lon.Value };

                list = list.Where(r =>
                {
                    if (!r.Latitude.HasValue || !r.Longitude.HasValue)
                        return false;

                    var restaurantLocation = new GeoLocation { Latitude = r.Latitude.Value, Longitude = r.Longitude.Value };
                    var distance = _geoService.CalculateDistance(userLocation, restaurantLocation);

                    return distance <= 10; // 10 km radius
                }).ToList();
            }

            var menuTypes = await _context.Restaurants
                                 .Select(r => r.MenuType)
                                 .Distinct()
                                 .OrderBy(m => m)
                                 .ToListAsync();

            ViewBag.MenuTypes = new SelectList(menuTypes);

            var averageRatings = list.ToDictionary(
                r => r.Id,
                r => r.Reviews.Any() ? r.Reviews.Average(rv => rv.Rating) : 0);

            ViewData["AverageRatings"] = averageRatings;

            return View(list);
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
