using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    [AuthorizeOrganiser]
    public class OrganiserController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;

        public OrganiserController(IRaceDayApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        // 3.1 Dashboard summary of all events created, total enrolments, upcoming dates
        public async Task<IActionResult> Dashboard()
        {
            var events = await _apiClient.GetEventsAsync();
            var myEvents = events; // filtered by logged-in organiser on API

            var upcoming = myEvents
                .Where(e => e.EventDate >= DateTime.Today && e.Status != "Cancelled")
                .OrderBy(e => e.EventDate)
                .FirstOrDefault();

            var model = new OrganiserDashboardViewModel
            {
                OrganiserName = HttpContext.Session.GetString("FullName") ?? "Organiser",
                TotalEvents = myEvents.Count,
                TotalEnrolments = myEvents.Sum(e => e.EnrolmentsCount),
                ActiveEventsCount = myEvents.Count(e => e.Status == "Upcoming" || e.Status == "Open"),
                NextUpcomingEvent = upcoming,
                Events = myEvents.OrderByDescending(e => e.EventDate).ToList()
            };

            return View(model);
        }

        // 3.2 Events management page to list all events
        public async Task<IActionResult> Events()
        {
            var events = await _apiClient.GetEventsAsync();
            return View(events);
        }

        // 3.2 & 3.3 Create event with required fields and banner upload
        [HttpGet]
        public IActionResult CreateEvent()
        {
            return View(new CreateEventViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEvent(CreateEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.CreateEventAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Could not create event.");
                return View(model);
            }

            TempData["SuccessMessage"] = $"Event '{model.EventName}' created successfully with Azure Blob banner image!";
            return RedirectToAction("Dashboard");
        }

        [HttpGet]
        public async Task<IActionResult> EditEvent(int id)
        {
            var ev = await _apiClient.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Events");
            }

            var model = new EditEventViewModel
            {
                EventId = ev.EventId,
                EventName = ev.EventName,
                Description = ev.Description,
                EventDate = ev.EventDate,
                StartTime = ev.StartTime,
                Location = ev.Location,
                Province = ev.Province,
                Status = ev.Status,
                EventTypeId = ev.EventTypeId,
                ExistingBannerUrl = ev.BannerImageUrl
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEvent(int id, EditEventViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.UpdateEventAsync(id, model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Failed to update event.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Event details and banner image updated successfully.";
            return RedirectToAction("Events");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var result = await _apiClient.DeleteEventAsync(id);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Event was deleted.";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Could not delete event.";
            }

            return RedirectToAction("Events");
        }

        // 3.4 Category management page to add age or distance categories to each event
        [HttpGet]
        public async Task<IActionResult> Categories(int id)
        {
            var ev = await _apiClient.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Events");
            }

            ViewBag.Event = ev;
            return View(new CreateCategoryViewModel { EventId = id, EventName = ev.EventName });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(int id, CreateCategoryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var ev = await _apiClient.GetEventByIdAsync(id);
                ViewBag.Event = ev;
                return View("Categories", model);
            }

            var result = await _apiClient.CreateCategoryAsync(id, model);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Error ?? "Failed to add category.";
            }
            else
            {
                TempData["SuccessMessage"] = $"Category '{model.CategoryName}' ({model.DistanceKm}km) added successfully!";
            }

            return RedirectToAction("Categories", new { id });
        }

        // 3.5 Enrolments page to view all Participants who enrolled for an event
        public async Task<IActionResult> Enrolments(int id)
        {
            var ev = await _apiClient.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Events");
            }

            var enrolments = await _apiClient.GetEventEnrolmentsAsync(id);

            var model = new EventEnrolmentsViewModel
            {
                EventId = id,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                Location = ev.Location,
                Enrolments = enrolments
            };

            return View(model);
        }

        // 3.6 Results entry page to capture and publish finish times and finishing positions
        [HttpGet]
        public async Task<IActionResult> ResultsEntry(int id)
        {
            var ev = await _apiClient.GetEventByIdAsync(id);
            if (ev == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("Events");
            }

            var enrolments = await _apiClient.GetEventEnrolmentsAsync(id);

            var model = new ResultsEntryViewModel
            {
                EventId = id,
                EventName = ev.EventName,
                EventDate = ev.EventDate,
                Participants = enrolments.Select(e => new CaptureResultItemViewModel
                {
                    EnrolmentId = e.EnrolmentId,
                    ParticipantName = e.ParticipantName,
                    RaceNumber = e.RaceNumber,
                    CategoryName = e.CategoryName,
                    FinishTime = e.FinishTime ?? "01:15:30",
                    OverallPosition = e.OverallPosition,
                    Status = "Finished"
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PublishResult(int eventId, int enrolmentId, string finishTime, int? overallPosition, int? categoryPosition, string status)
        {
            var result = await _apiClient.CaptureResultAsync(enrolmentId, finishTime, overallPosition, categoryPosition, status);
            if (result.Success)
            {
                TempData["SuccessMessage"] = $"Result published: {finishTime} (Position #{overallPosition})";
            }
            else
            {
                TempData["ErrorMessage"] = result.Error ?? "Could not save result.";
            }

            return RedirectToAction("ResultsEntry", new { id = eventId });
        }
    }
}
