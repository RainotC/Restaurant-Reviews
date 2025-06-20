using System.Net.Http;
using System.Text.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Globalization;

namespace Restaurants.Services
{
	public class GeolocationService
	{
		private readonly HttpClient _httpClient;

		public GeolocationService(HttpClient httpClient)
		{
			_httpClient = httpClient;
			_httpClient.DefaultRequestHeaders.Add("User-Agent", "YourAppName/1.0");
		}

		public async Task<(double lat, double lon)?> GetCoordinatesAsync(string address)
		{
			var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

			var response = await _httpClient.GetStringAsync(url);

			var results = JsonSerializer.Deserialize<List<NominatimResult>>(response);

			if (results != null && results.Count > 0)
			{
                if (double.TryParse(results[0].lat, NumberStyles.Any, CultureInfo.InvariantCulture, out var lat) &&
    double.TryParse(results[0].lon, NumberStyles.Any, CultureInfo.InvariantCulture, out var lon))
                {
					return (lat, lon);
				}
			}

			return null; 
		}
		public static double ToRadians(double deg) => deg * (Math.PI / 180);
		public double CalculateDistance(GeoLocation loc1, GeoLocation loc2)
		{
			double R = 6371;
			var dLat = ToRadians(loc2.Latitude - loc1.Latitude);
			var dLon = ToRadians(loc2.Longitude - loc1.Longitude);
			var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
					Math.Cos(ToRadians(loc1.Latitude)) * Math.Cos(ToRadians(loc2.Latitude)) *
					Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
			var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
			return R * c;
		}
	}

	public class GeoLocation
	{
		public double Latitude { get; set; }
		public double Longitude { get; set; }
	}

	public class NominatimResult
	{
		public string Place_Id { get; set; }
		public string Display_Name { get; set; }
		public string lat { get; set; }
		public string lon { get; set; }
	}


}