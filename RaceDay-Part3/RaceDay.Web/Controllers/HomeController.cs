using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public HomeController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Index(
            string? searchTerm, 
            int? eventTypeId, 
            string? province, 
            DateTime? date,
            decimal? minDistance,
            decimal? maxDistance)
        {
            var events = await _apiClient.GetEventsAsync(date, eventTypeId, province, searchTerm);

            if (minDistance.HasValue)
            {
                events = events.Where(e => e.Categories.Any(c => c.DistanceKm >= minDistance.Value)).ToList();
            }

            if (maxDistance.HasValue)
            {
                events = events.Where(e => e.Categories.Any(c => c.DistanceKm <= maxDistance.Value)).ToList();
            }

            var viewModel = new EventListViewModel
            {
                Events = events,
                SearchTerm = searchTerm,
                SelectedEventTypeId = eventTypeId,
                SelectedProvince = province,
                SelectedDate = date,
                MinDistanceKm = minDistance,
                MaxDistanceKm = maxDistance
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}
