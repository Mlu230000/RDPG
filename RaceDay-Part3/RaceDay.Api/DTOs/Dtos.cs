using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs
{
    // --- Auth DTOs ---
    public class RegisterDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Range(1, 2, ErrorMessage = "RoleId must be 1 (Organiser) or 2 (Participant)")]
        public int RoleId { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }
    }

    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = null!;
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateProfileDto
    {
        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? ProfilePictureUrl { get; set; }
    }

    // --- Blob Upload DTO ---
    public class BlobUploadResponseDto
    {
        public string Url { get; set; } = string.Empty;
        public string BlobName { get; set; } = string.Empty;
        public string ContainerName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
    }

    // --- Event DTOs ---
    public class CreateEventDto
    {
        [Required]
        [MaxLength(150)]
        public string EventName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public DateTime EventDate { get; set; }

        public string? StartTime { get; set; } // "06:00"

        [Required]
        [MaxLength(150)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Province { get; set; }

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        [Required]
        public int EventTypeId { get; set; }
    }

    public class UpdateEventDto
    {
        [MaxLength(150)]
        public string? EventName { get; set; }

        public string? Description { get; set; }

        public DateTime? EventDate { get; set; }

        public string? StartTime { get; set; }

        [MaxLength(150)]
        public string? Location { get; set; }

        [MaxLength(50)]
        public string? Province { get; set; }

        [MaxLength(500)]
        public string? BannerImageUrl { get; set; }

        public int? EventTypeId { get; set; }

        [RegularExpression("^(Upcoming|Open|Closed|Completed|Cancelled)$", ErrorMessage = "Status must be 'Upcoming', 'Open', 'Closed', 'Completed', or 'Cancelled'.")]
        public string? Status { get; set; }
    }

    public class EventResponseDto
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
        public string Status { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string? OrganiserName { get; set; }
        public int CategoriesCount { get; set; }
        public int EnrolmentsCount { get; set; }
        public List<CategoryResponseDto> Categories { get; set; } = new();
    }

    public class WeatherResponseDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Forecast { get; set; } = string.Empty;
        public int TemperatureCelsius { get; set; }
        public int FeelsLikeCelsius { get; set; }
        public int HumidityPercent { get; set; }
        public string WindSpeed { get; set; } = string.Empty;
        public string AirQualityIndex { get; set; } = string.Empty;
        public string Sunrise { get; set; } = string.Empty;
        public string HydrationWarning { get; set; } = string.Empty;
    }

    // --- Category DTOs ---
    public class CreateCategoryDto
    {
        [Required]
        [MaxLength(50)]
        public string CategoryName { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 999.99)]
        public decimal DistanceKm { get; set; }

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        [Required]
        [Range(0, 99999.99)]
        public decimal EntryFee { get; set; }

        public int? MaxParticipants { get; set; }

        public string? RouteName { get; set; }

        public int? ElevationGainM { get; set; }
    }

    public class UpdateCategoryDto
    {
        [MaxLength(50)]
        public string? CategoryName { get; set; }

        public decimal? DistanceKm { get; set; }

        public int? MinAge { get; set; }

        public int? MaxAge { get; set; }

        public decimal? EntryFee { get; set; }

        public int? MaxParticipants { get; set; }
    }

    public class CategoryResponseDto
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
        public RouteResponseDto? Route { get; set; }
    }

    public class RouteResponseDto
    {
        public int RouteId { get; set; }
        public int CategoryId { get; set; }
        public string RouteName { get; set; } = string.Empty;
        public string? StartPoint { get; set; }
        public string? EndPoint { get; set; }
        public int? ElevationGainM { get; set; }
        public string? RouteMapUrl { get; set; }
    }

    // --- Enrolment DTOs ---
    public class EnrolDto
    {
        [RegularExpression("^(Pending|Paid|Refunded)$", ErrorMessage = "PaymentStatus must be 'Pending', 'Paid', or 'Refunded'.")]
        public string? PaymentStatus { get; set; } = "Paid";
    }

    public class EnrolmentResponseDto
    {
        public int EnrolmentId { get; set; }
        public int ParticipantId { get; set; }
        public int CategoryId { get; set; }
        public string? RaceNumber { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
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

    // --- Result DTOs ---
    public class CaptureResultDto
    {
        [Required]
        public string FinishTime { get; set; } = string.Empty; // "HH:mm:ss"

        public int? OverallPosition { get; set; }

        public int? CategoryPosition { get; set; }

        [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be 'Finished', 'DNF', or 'DSQ'.")]
        public string? Status { get; set; } = "Finished";
    }

    public class UpdateResultDto
    {
        public string? FinishTime { get; set; }

        public int? OverallPosition { get; set; }

        public int? CategoryPosition { get; set; }

        [RegularExpression("^(Finished|DNF|DSQ)$", ErrorMessage = "Status must be 'Finished', 'DNF', or 'DSQ'.")]
        public string? Status { get; set; }
    }

    public class ResultResponseDto
    {
        public int ResultId { get; set; }
        public int EnrolmentId { get; set; }
        public string? FinishTime { get; set; }
        public int? OverallPosition { get; set; }
        public int? CategoryPosition { get; set; }
        public int TotalFinishers { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RaceNumber { get; set; }
        public string? ParticipantName { get; set; }
        public string? CategoryName { get; set; }
        public decimal? DistanceKm { get; set; }
        public string? EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public string? Location { get; set; }
    }
}
