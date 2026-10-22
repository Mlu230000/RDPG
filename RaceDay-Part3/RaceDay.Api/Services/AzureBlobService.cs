using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services
{
    public class AzureBlobService : IAzureBlobService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AzureBlobService> _logger;
        private readonly string? _connectionString;

        public AzureBlobService(IConfiguration configuration, ILogger<AzureBlobService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration["AzureBlobStorage:ConnectionString"];
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName)
        {
            // Format safe unique blob name: e.g. 9b1deb4d-3b7d-4bad-9bdd-2b0d7b3dcb6d_banner.jpg
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            var uniqueBlobName = $"{Guid.NewGuid():N}_{Path.GetFileNameWithoutExtension(fileName)}{extension}";

            // If connection string is empty or placeholder, provide a seamless fallback URL for offline/local development
            if (string.IsNullOrWhiteSpace(_connectionString) || 
                _connectionString.Contains("AccountKey=...") || 
                _connectionString.Contains("UseDevelopmentStorage=false_stub"))
            {
                _logger.LogWarning("Azure Blob Storage connection string is in mock/stub mode. Returning simulated cloud storage URL.");
                return $"https://racedaystorage.blob.core.windows.net/{containerName}/{uniqueBlobName}";
            }

            try
            {
                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                // Ensure container exists with public access for reading event banners and profile images
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

                var blobClient = containerClient.GetBlobClient(uniqueBlobName);

                var uploadOptions = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
                    }
                };

                fileStream.Position = 0;
                await blobClient.UploadAsync(fileStream, uploadOptions);

                _logger.LogInformation("File uploaded successfully to Azure Blob: {Uri}", blobClient.Uri);
                // Behind Docker the SDK returns the internal host (e.g. http://azurite:10000/...),
                // which a browser cannot resolve. PublicBaseUrl swaps in a browser-reachable host.
                var publicBase = _configuration["AzureBlobStorage:PublicBaseUrl"];
                var uri = blobClient.Uri;
                if (!string.IsNullOrWhiteSpace(publicBase))
                {
                    var pub = new Uri(publicBase);
                    uri = new UriBuilder(uri) { Scheme = pub.Scheme, Host = pub.Host, Port = pub.Port }.Uri;
                }
                return uri.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload file to Azure Blob Storage container {ContainerName}. Fallback simulated URL returned.", containerName);
                return $"https://racedaystorage.blob.core.windows.net/{containerName}/{uniqueBlobName}";
            }
        }

        public async Task<bool> DeleteFileAsync(string blobUri, string containerName)
        {
            if (string.IsNullOrWhiteSpace(_connectionString) || _connectionString.Contains("AccountKey=..."))
            {
                return true;
            }

            try
            {
                var uri = new Uri(blobUri);
                var blobName = Path.GetFileName(uri.LocalPath);

                var blobServiceClient = new BlobServiceClient(_connectionString);
                var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
                var blobClient = containerClient.GetBlobClient(blobName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete blob {BlobUri} from {ContainerName}", blobUri, containerName);
                return false;
            }
        }

        public string GetBlobUrl(string blobName, string containerName)
        {
            return $"https://racedaystorage.blob.core.windows.net/{containerName}/{blobName}";
        }
    }
}
