using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RaceDay.Web.Models;

namespace RaceDay.Web.Services
{
    public interface IRaceDayApiClient
    {
        Task<(bool Success, string? Token, string? Role, string? Name, int? UserId, string? Error)> LoginAsync(string email, string password);
        Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model);
        Task<List<EventItemViewModel>> GetEventsAsync(DateTime? date = null, int? eventTypeId = null, string? province = null, string? search = null);
        Task<EventItemViewModel?> GetEventByIdAsync(int eventId);
        Task<(bool Success, int? CreatedId, string? Error)> CreateEventAsync(CreateEventViewModel model);
        Task<(bool Success, string? Error)> UpdateEventAsync(int eventId, EditEventViewModel model);
        Task<(bool Success, string? Error)> DeleteEventAsync(int eventId);
        Task<(bool Success, string? BannerUrl, string? Error)> UploadEventBannerAsync(int eventId, IFormFile file);
        Task<List<CategoryItemViewModel>> GetCategoriesByEventAsync(int eventId);
        Task<(bool Success, string? Error)> CreateCategoryAsync(int eventId, CreateCategoryViewModel model);
        Task<List<EnrolmentItemViewModel>> GetEventEnrolmentsAsync(int eventId);
        Task<(bool Success, string? Error)> EnrolInEventAsync(int categoryId);
        Task<List<EnrolmentItemViewModel>> GetMyEnrolmentsAsync();
        Task<List<ParticipantResultViewModel>> GetMyResultsAsync();
        Task<(bool Success, string? Error)> CaptureResultAsync(int enrolmentId, string finishTime, int? overallPosition, int? categoryPosition, string status);
        Task<ProfileViewModel?> GetProfileAsync();
        Task<(bool Success, string? Error)> UpdateProfileAsync(ProfileViewModel model);
        Task<(bool Success, string? PictureUrl, string? Error)> UploadProfilePictureAsync(int userId, IFormFile file);
    }

    public class RaceDayApiClient : IRaceDayApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<RaceDayApiClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public RaceDayApiClient(
            HttpClient httpClient, 
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration,
            ILogger<RaceDayApiClient> logger)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;

            var baseUrl = configuration["RaceDayApi:BaseUrl"] ?? "http://localhost:5000";
            if (!baseUrl.EndsWith("/")) baseUrl += "/";
            _httpClient.BaseAddress = new Uri(baseUrl);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private void AttachBearerToken()
        {
            var token = _httpContextAccessor.HttpContext?.Session.GetString("AuthToken");
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
        }

        public async Task<(bool Success, string? Token, string? Role, string? Name, int? UserId, string? Error)> LoginAsync(string email, string password)
        {
            try
            {
                var content = new StringContent(JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/Auth/login", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    var errorObj = TryParseJson(responseBody);
                    return (false, null, null, null, null, errorObj ?? "Invalid email or password.");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var token = doc.RootElement.GetProperty("token").GetString();
                var userElem = doc.RootElement.GetProperty("user");
                var role = userElem.GetProperty("role").GetString();
                var fullName = userElem.GetProperty("fullName").GetString();
                var userId = userElem.GetProperty("userId").GetInt32();

                return (true, token, role, fullName, userId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to RaceDay API login endpoint.");
                return (false, null, null, null, null, "Could not connect to RaceDay API server.");
            }
        }

        public async Task<(bool Success, string? Error)> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                var payload = new
                {
                    fullName = model.FullName,
                    email = model.Email,
                    password = model.Password,
                    roleId = model.RoleId,
                    phone = model.Phone
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/Auth/register", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, TryParseJson(responseBody) ?? "Registration failed. Please check inputs.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user via RaceDay API.");
                return (false, "Could not reach RaceDay API server.");
            }
        }

        public async Task<List<EventItemViewModel>> GetEventsAsync(DateTime? date = null, int? eventTypeId = null, string? province = null, string? search = null)
        {
            try
            {
                var queryParams = new List<string>();
                if (date.HasValue) queryParams.Add($"date={date.Value:yyyy-MM-dd}");
                if (eventTypeId.HasValue) queryParams.Add($"eventTypeId={eventTypeId.Value}");
                if (!string.IsNullOrEmpty(province)) queryParams.Add($"province={Uri.EscapeDataString(province)}");

                var url = "api/Events";
                if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode) return new List<EventItemViewModel>();

                var json = await response.Content.ReadAsStringAsync();
                var events = JsonSerializer.Deserialize<List<EventItemViewModel>>(json, _jsonOptions) ?? new List<EventItemViewModel>();

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var s = search.ToLowerInvariant();
                    events = events.Where(e => e.EventName.ToLowerInvariant().Contains(s) || 
                                               e.Location.ToLowerInvariant().Contains(s) || 
                                               (e.Description != null && e.Description.ToLowerInvariant().Contains(s))).ToList();
                }

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching events from RaceDay API.");
                return new List<EventItemViewModel>();
            }
        }

        public async Task<EventItemViewModel?> GetEventByIdAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Events/{eventId}");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                var ev = JsonSerializer.Deserialize<EventItemViewModel>(json, _jsonOptions);

                // Fetch categories
                if (ev != null)
                {
                    ev.Categories = await GetCategoriesByEventAsync(eventId);
                }

                return ev;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching event {EventId} from API", eventId);
                return null;
            }
        }

        public async Task<(bool Success, int? CreatedId, string? Error)> CreateEventAsync(CreateEventViewModel model)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    eventName = model.EventName,
                    description = model.Description,
                    eventDate = model.EventDate,
                    startTime = model.StartTime,
                    location = model.Location,
                    province = model.Province,
                    eventTypeId = model.EventTypeId
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("api/Events", content);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, TryParseJson(responseBody) ?? "Failed to create event.");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var createdId = doc.RootElement.GetProperty("eventId").GetInt32();

                // If initial distance provided, create a default category for it
                if (model.InitialDistanceKm > 0)
                {
                    await CreateCategoryAsync(createdId, new CreateCategoryViewModel
                    {
                        EventId = createdId,
                        CategoryName = $"{model.InitialDistanceKm}km Open Race",
                        DistanceKm = model.InitialDistanceKm,
                        EntryFee = 150m,
                        MaxParticipants = 500
                    });
                }

                // If banner file was uploaded, upload to Azure Blob Storage via API
                if (model.BannerFile != null && model.BannerFile.Length > 0)
                {
                    await UploadEventBannerAsync(createdId, model.BannerFile);
                }

                return (true, createdId, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event via API");
                return (false, null, "Could not reach RaceDay API.");
            }
        }

        public async Task<(bool Success, string? Error)> UpdateEventAsync(int eventId, EditEventViewModel model)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    eventName = model.EventName,
                    description = model.Description,
                    eventDate = model.EventDate,
                    startTime = model.StartTime,
                    location = model.Location,
                    province = model.Province,
                    status = model.Status,
                    eventTypeId = model.EventTypeId
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"api/Events/{eventId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (false, TryParseJson(responseBody) ?? "Failed to update event.");
                }

                if (model.BannerFile != null && model.BannerFile.Length > 0)
                {
                    await UploadEventBannerAsync(eventId, model.BannerFile);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event via API");
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<(bool Success, string? Error)> DeleteEventAsync(int eventId)
        {
            AttachBearerToken();
            try
            {
                var response = await _httpClient.DeleteAsync($"api/Events/{eventId}");
                return (response.IsSuccessStatusCode, response.IsSuccessStatusCode ? null : "Failed to delete event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", eventId);
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<(bool Success, string? BannerUrl, string? Error)> UploadEventBannerAsync(int eventId, IFormFile file)
        {
            AttachBearerToken();
            try
            {
                using var form = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
                form.Add(streamContent, "file", file.FileName);

                var response = await _httpClient.PostAsync($"api/Events/{eventId}/banner", form);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, TryParseJson(responseBody) ?? "Failed to upload banner to Azure Blob Storage.");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var bannerUrl = doc.RootElement.TryGetProperty("bannerImageUrl", out var p) ? p.GetString() : null;
                return (true, bannerUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading banner for event {EventId}", eventId);
                return (false, null, "Could not upload banner via RaceDay API.");
            }
        }

        public async Task<List<CategoryItemViewModel>> GetCategoriesByEventAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/Categories/event/{eventId}");
                if (!response.IsSuccessStatusCode) return new List<CategoryItemViewModel>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<CategoryItemViewModel>>(json, _jsonOptions) ?? new List<CategoryItemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching categories for event {EventId}", eventId);
                return new List<CategoryItemViewModel>();
            }
        }

        public async Task<(bool Success, string? Error)> CreateCategoryAsync(int eventId, CreateCategoryViewModel model)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    categoryName = model.CategoryName,
                    distanceKm = model.DistanceKm,
                    minAge = model.MinAge,
                    maxAge = model.MaxAge,
                    entryFee = model.EntryFee,
                    maxParticipants = model.MaxParticipants
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/Categories/event/{eventId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (false, TryParseJson(responseBody) ?? "Failed to create category.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category for event {EventId}", eventId);
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<List<EnrolmentItemViewModel>> GetEventEnrolmentsAsync(int eventId)
        {
            AttachBearerToken();
            try
            {
                var response = await _httpClient.GetAsync($"api/Enrolments/event/{eventId}");
                if (!response.IsSuccessStatusCode) return new List<EnrolmentItemViewModel>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<EnrolmentItemViewModel>>(json, _jsonOptions) ?? new List<EnrolmentItemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting enrolments for event {EventId}", eventId);
                return new List<EnrolmentItemViewModel>();
            }
        }

        public async Task<(bool Success, string? Error)> EnrolInEventAsync(int categoryId)
        {
            AttachBearerToken();
            try
            {
                var payload = new { paymentStatus = "Paid" };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/Enrolments/category/{categoryId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (false, TryParseJson(responseBody) ?? "Failed to enrol in event.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enrolling in category {CategoryId}", categoryId);
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<List<EnrolmentItemViewModel>> GetMyEnrolmentsAsync()
        {
            AttachBearerToken();
            try
            {
                var response = await _httpClient.GetAsync("api/Enrolments/my");
                if (!response.IsSuccessStatusCode) return new List<EnrolmentItemViewModel>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<EnrolmentItemViewModel>>(json, _jsonOptions) ?? new List<EnrolmentItemViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user enrolments");
                return new List<EnrolmentItemViewModel>();
            }
        }

        public async Task<List<ParticipantResultViewModel>> GetMyResultsAsync()
        {
            AttachBearerToken();
            try
            {
                var response = await _httpClient.GetAsync("api/Results/my");
                if (!response.IsSuccessStatusCode) return new List<ParticipantResultViewModel>();

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<ParticipantResultViewModel>>(json, _jsonOptions) ?? new List<ParticipantResultViewModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting my results");
                return new List<ParticipantResultViewModel>();
            }
        }

        public async Task<(bool Success, string? Error)> CaptureResultAsync(int enrolmentId, string finishTime, int? overallPosition, int? categoryPosition, string status)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    finishTime,
                    overallPosition,
                    categoryPosition,
                    status = string.IsNullOrEmpty(status) ? "Finished" : status
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"api/Results/enrolment/{enrolmentId}", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (false, TryParseJson(responseBody) ?? "Failed to capture result.");
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing result for enrolment {EnrolmentId}", enrolmentId);
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<ProfileViewModel?> GetProfileAsync()
        {
            AttachBearerToken();
            try
            {
                var response = await _httpClient.GetAsync("api/Users/me");
                if (!response.IsSuccessStatusCode) return null;

                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<ProfileViewModel>(json, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile");
                return null;
            }
        }

        public async Task<(bool Success, string? Error)> UpdateProfileAsync(ProfileViewModel model)
        {
            AttachBearerToken();
            try
            {
                var payload = new
                {
                    fullName = model.FullName,
                    phone = model.Phone
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync("api/Users/me", content);

                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return (false, TryParseJson(responseBody) ?? "Failed to update profile.");
                }

                if (model.ProfilePictureFile != null && model.ProfilePictureFile.Length > 0)
                {
                    await UploadProfilePictureAsync(model.UserId, model.ProfilePictureFile);
                }

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return (false, "Could not reach RaceDay API.");
            }
        }

        public async Task<(bool Success, string? PictureUrl, string? Error)> UploadProfilePictureAsync(int userId, IFormFile file)
        {
            AttachBearerToken();
            try
            {
                using var form = new MultipartFormDataContent();
                using var stream = file.OpenReadStream();
                using var streamContent = new StreamContent(stream);
                streamContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "image/jpeg");
                form.Add(streamContent, "file", file.FileName);

                var response = await _httpClient.PostAsync($"api/Users/{userId}/profile-picture", form);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return (false, null, TryParseJson(responseBody) ?? "Failed to upload profile picture to Azure Blob Storage.");
                }

                using var doc = JsonDocument.Parse(responseBody);
                var pictureUrl = doc.RootElement.TryGetProperty("profilePictureUrl", out var p) ? p.GetString() : null;
                return (true, pictureUrl, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading profile picture");
                return (false, null, "Could not reach RaceDay API.");
            }
        }

        private static string? TryParseJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("error", out var e)) return e.GetString();
                if (doc.RootElement.TryGetProperty("title", out var t)) return t.GetString();
            }
            catch { }
            return null;
        }
    }
}
