using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IRaceDayApiClient _apiClient;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IRaceDayApiClient apiClient, ILogger<AccountController> logger)
        {
            _apiClient = apiClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.LoginAsync(model.Email, model.Password);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Invalid login credentials.");
                return View(model);
            }

            // Store in ASP.NET Core Session
            HttpContext.Session.SetString("AuthToken", result.Token ?? string.Empty);
            HttpContext.Session.SetString("Role", result.Role ?? "Participant");
            HttpContext.Session.SetString("FullName", result.Name ?? "Athlete");
            HttpContext.Session.SetInt32("UserId", result.UserId ?? 0);
            HttpContext.Session.SetString("Email", model.Email);

            TempData["SuccessMessage"] = $"Welcome back, {result.Name}!";

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            // Role-specific redirect
            if (string.Equals(result.Role, "Organiser", StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Dashboard", "Organiser");
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _apiClient.RegisterAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Error ?? "Registration could not be completed.");
                return View(model);
            }

            TempData["SuccessMessage"] = "Registration successful! Please log in with your credentials.";
            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            TempData["InfoMessage"] = "You have been successfully logged out.";
            return RedirectToAction("Index", "Home");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
