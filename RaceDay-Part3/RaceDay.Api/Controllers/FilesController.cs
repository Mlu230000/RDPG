using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;
using Swashbuckle.AspNetCore.Annotations;

namespace RaceDay.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    [Produces("application/json")]
    public class FilesController : ControllerBase
    {
        private readonly IAzureBlobService _blobService;
        private readonly ILogger<FilesController> _logger;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB limit

        public FilesController(IAzureBlobService blobService, ILogger<FilesController> logger)
        {
            _blobService = blobService;
            _logger = logger;
        }

        /// <summary>
        /// Uploads an image file to Azure Blob Storage under the specified container (e.g. event-banners or profile-pictures).
        /// </summary>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Uploads an image to Azure Blob Storage", Description = "Stores the uploaded file in Azure Blob Storage and returns its public URL.")]
        [ProducesResponseType(typeof(BlobUploadResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BlobUploadResponseDto>> UploadFile(IFormFile file, [FromQuery] string container = "event-banners")
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file was provided for upload." });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { error = "File size exceeds 10MB limit." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                return BadRequest(new { error = $"Invalid file extension {extension}. Allowed formats: JPG, PNG, WEBP, GIF." });
            }

            // Normalise container name
            var cleanContainer = container.ToLowerInvariant().Trim();
            if (cleanContainer != "event-banners" && cleanContainer != "profile-pictures")
            {
                cleanContainer = "event-banners";
            }

            try
            {
                using var stream = file.OpenReadStream();
                var blobUrl = await _blobService.UploadFileAsync(stream, file.FileName, file.ContentType, cleanContainer);

                return Ok(new BlobUploadResponseDto
                {
                    Url = blobUrl,
                    BlobName = Path.GetFileName(new Uri(blobUrl).LocalPath),
                    ContainerName = cleanContainer,
                    ContentType = file.ContentType,
                    SizeBytes = file.Length
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing file upload to Azure Blob Storage.");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while uploading file to Azure Blob Storage." });
            }
        }
    }
}
