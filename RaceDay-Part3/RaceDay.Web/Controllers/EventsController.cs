using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class EventsController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public EventsController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<IActionResult> Details(int id)
        {
            var ev = await _apiClient.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "The requested event could not be found.";
                return RedirectToAction("Index", "Home");
            }

            return View(ev);
        }
    }
}
