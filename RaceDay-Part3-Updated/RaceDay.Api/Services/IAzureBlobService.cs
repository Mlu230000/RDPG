namespace RaceDay.Api.Services
{
    public interface IAzureBlobService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string containerName);
        Task<bool> DeleteFileAsync(string blobUri, string containerName);
        string GetBlobUrl(string blobName, string containerName);
    }
}
