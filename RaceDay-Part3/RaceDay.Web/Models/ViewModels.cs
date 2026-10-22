using System.ComponentModel.DataAnnotations;

namespace RaceDay.Web.Models
{
    // --- Auth ViewModels ---
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm your password")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select your account type")]
        [Display(Name = "Register As")]
        public int RoleId { get; set; } = 2; // Default to Participant

        [Phone]
        [MaxLength(20)]
        [Display(Name = "Mobile Phone")]
        public string? Phone { get; set; }
    }

    // --- Event ViewModels ---
    public class EventItemViewModel
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public int EventTypeId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? StartTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? Province { get; set; }
        public string? BannerImageUrl { get; set; }
        public string Status { get; set; } = "Upcoming";
        public string EventType { get; set; } = string.Empty;
        public string? OrganiserName { get; set; }
        public int CategoriesCount { get; set; }
        public int EnrolmentsCount { get; set; }
        public List<CategoryItemViewModel> Categories { get; set; } = new();
    }

    public class EventListViewModel
    {
        public List<EventItemViewModel> Events { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? SelectedEventTypeId { get; set; }
        public string? SelectedProvince { get; set; }
        public DateTime? SelectedDate { get; set; }
        public decimal? MinDistanceKm { get; set; }
        public decimal? MaxDistanceKm { get; set; }
    }

    public class CreateEventViewModel
    {
        [Required(ErrorMessage = "Event name is required")]
        [MaxLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Event Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Race Date")]
        public DateTime EventDate { get; set; } = DateTime.Today.AddDays(30);

        [Display(Name = "Start Time (HH:mm)")]
        public string? StartTime { get; set; } = "06:00";

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(150)]
        [Display(Name = "Race Location")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Province / Region")]
        public string? Province { get; set; } = "Gauteng";

        [Required(ErrorMessage = "Event type is required")]
        [Display(Name = "Discipline / Event Type")]
        public int EventTypeId { get; set; } = 1; // 1 = Running, 2 = Cycling, 3 = Walking

        [Display(Name = "Initial Distance (km)")]
        [Range(0.1, 999.99, ErrorMessage = "Enter a valid distance")]
        public decimal InitialDistanceKm { get; set; } = 10.0m;

        [Display(Name = "Event Banner Image")]
        public IFormFile? BannerFile { get; set; }
    }

    public class EditEventViewModel
    {
        public int EventId { get; set; }

        [Required(ErrorMessage = "Event name is required")]
        [MaxLength(150)]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Display(Name = "Event Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Event date is required")]
        [DataType(DataType.Date)]
        [Display(Name = "Race Date")]
        public DateTime EventDate { get; set; }

        [Display(Name = "Start Time (HH:mm)")]
        public string? StartTime { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [MaxLength(150)]
        [Display(Name = "Race Location")]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        [Display(Name = "Province")]
        public string? Province { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Upcoming";

        [Required(ErrorMessage = "Event type is required")]
        [Display(Name = "Event Type")]
        public int EventTypeId { get; set; }

        public string? ExistingBannerUrl { get; set; }

        [Display(Name = "Update Banner Image")]
        public IFormFile? BannerFile { get; set; }
    }

    // --- Category ViewModels ---
    public class CategoryItemViewModel
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public decimal EntryFee { get; set; }
        public int? MaxParticipants { get; set; }
        public int EnrolmentsCount { get; set; }
    }

    public class CreateCategoryViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Category name is required")]
        [MaxLength(50)]
        [Display(Name = "Category Name (e.g. 10km Classic, 21.1km Half Marathon)")]
        public string CategoryName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Distance is required")]
        [Range(0.1, 999.99, ErrorMessage = "Distance must be between 0.1 and 999.99 km")]
        [Display(Name = "Distance (km)")]
        public decimal DistanceKm { get; set; } = 10.0m;

        [Display(Name = "Minimum Age (optional)")]
        [Range(1, 120)]
        public int? MinAge { get; set; }

        [Display(Name = "Maximum Age (optional)")]
        [Range(1, 120)]
        public int? MaxAge { get; set; }

        [Required(ErrorMessage = "Entry fee is required")]
        [Range(0, 99999.99, ErrorMessage = "Entry fee must be positive")]
        [Display(Name = "Entry Fee (ZAR R)")]
        public decimal EntryFee { get; set; } = 150.0m;

        [Display(Name = "Max Participants / Cap (optional)")]
        public int? MaxParticipants { get; set; } = 500;
    }

    // --- Enrolment & Results ViewModels ---
    public class EnrolmentItemViewModel
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }
        public string? RaceNumber { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string PaymentStatus { get; set; } = "Paid"; // 'Paid', 'Pending', 'Refunded'
        public string? ParticipantName { get; set; }
        public string? ParticipantEmail { get; set; }
        public string? CategoryName { get; set; }
        public string? EventName { get; set; }
        public int EventId { get; set; }
        public decimal? EntryFee { get; set; }
        public decimal? DistanceKm { get; set; }
        public bool HasResult { get; set; }
        public string? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
    }

    public class EventEnrolmentsViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public List<EnrolmentItemViewModel> Enrolments { get; set; } = new();
    }

    public class CaptureResultItemViewModel
    {
        public int EnrolmentId { get; set; }
        public string? RaceNumber { get; set; }
        public string? ParticipantName { get; set; }
        public string? CategoryName { get; set; }
        public string? FinishTime { get; set; } // "01:23:45"
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
        public string Status { get; set; } = "Finished"; // Finished, DNF, DSQ
    }

    public class ResultsEntryViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public List<CaptureResultItemViewModel> Participants { get; set; } = new();
    }

    public class ParticipantResultViewModel
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public string FinishTime { get; set; } = string.Empty;
        public int OverallPosition { get; set; }
        public int CategoryPosition { get; set; }
        public int TotalFinishers { get; set; }
        public string Status { get; set; } = "Finished";
        public string? RaceNumber { get; set; }
    }

    // --- Profile ViewModel ---
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Full Name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Mobile Phone")]
        [Phone]
        public string? Phone { get; set; }

        public string Role { get; set; } = string.Empty;

        public string? ProfilePictureUrl { get; set; }

        [Display(Name = "Upload New Profile Picture")]
        public IFormFile? ProfilePictureFile { get; set; }
    }

    // --- Organiser Dashboard ViewModel ---
    public class OrganiserDashboardViewModel
    {
        public string OrganiserName { get; set; } = string.Empty;
        public int TotalEvents { get; set; }
        public int TotalEnrolments { get; set; }
        public int ActiveEventsCount { get; set; }
        public EventItemViewModel? NextUpcomingEvent { get; set; }
        public List<EventItemViewModel> Events { get; set; } = new();
    }
}
