using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [AuthorizeParticipant]
    public class ParticipantController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public ParticipantController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // 4.3 Logged-in participant enrols for an event with category selection
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enrol(int categoryId, int eventId)
        {
            var result = await _apiClient.EnrolInEventAsync(categoryId);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Error ?? "Could not complete enrolment.";
                return RedirectToAction("Details", "Events", new { id = eventId });
            }

            TempData["SuccessMessage"] = "Enrolment confirmed! You have been successfully registered for this event.";
            return RedirectToAction("MyEnrolments");
        }

        // 4.4 Personal view of all events enrolled for and enrolment status
        public async Task<IActionResult> MyEnrolments()
        {
            var enrolments = await _apiClient.GetMyEnrolmentsAsync();
            return View(enrolments);
        }

        // 4.5 My Results page showing personal race history with finishing position e.g. 47th out of 312 finishers
        public async Task<IActionResult> MyResults()
        {
            var results = await _apiClient.GetMyResultsAsync();
            return View(results);
        }

        // 4.6 Profile page where participant updates info and uploads Profile Picture to Azure Blob
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var profile = await _apiClient.GetProfileAsync();
            if (profile == null)
            {
                profile = new ProfileViewModel
                {
                    FullName = HttpContext.Session.GetString("FullName") ?? "Participant",
                    Email = HttpContext.Session.GetString("Email") ?? "athlete@raceday.co.za",
                    Role = "Participant"
                };
            }

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.UserId = HttpContext.Session.GetInt32("UserId") ?? 0;
            var result = await _apiClient.UpdateProfileAsync(model);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update profile.");
                return View(model);
            }

            HttpContext.Session.SetString("FullName", model.FullName);
            TempData["SuccessMessage"] = "Profile and photo updated successfully via Azure Blob Storage!";
            return RedirectToAction("Profile");
        }
    }
}
