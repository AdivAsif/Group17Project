namespace Group17profile.Services;

using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;
using Settings;

public interface IStorageService
{
    string? GetSasForFile(string containerName, string url, DateTimeOffset? expires = null);

    Task<Uri> SaveImageAsJpgBlob(string containerName, string fileName, Stream input,
        Dictionary<string, string>? metadata = null);
}

public class StorageService : IStorageService
{
    private readonly ConnectionStrings _connectionStrings;

    public StorageService(IOptions<ConnectionStrings> connectionStrings)
    {
        _connectionStrings = connectionStrings.Value;
    }

    public string? GetSasForFile(string containerName, string url, DateTimeOffset? expires = null)
    {
        url = GetFileFromUrl(url, containerName) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return null;
        var container = new BlobContainerClient(_connectionStrings.AzureBlobStorage, containerName);
        var blob = container.GetBlobClient(url);
        var urlWithSas = blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.Now.AddHours(3));
        return urlWithSas.Query;
    }

    public async Task<Uri> SaveImageAsJpgBlob(string containerName, string fileName, Stream input,
        Dictionary<string, string>? metadata = null)
    {
        using var image = await Image.LoadAsync(input);
        using var croppedImage = new MemoryStream();
        var clone = image.Clone(context =>
            context.Resize(new ResizeOptions {Mode = ResizeMode.Max, Size = new Size(1920, 1080)}));
        await clone.SaveAsJpegAsync(croppedImage);
        croppedImage.Position = 0;
        var blob = await GetBlobReference(containerName, fileName);
        await blob.UploadAsync(croppedImage, new BlobHttpHeaders {ContentType = "image/jpeg"});
        return blob.Uri;
    }

    private async Task<BlobClient> GetBlobReference(string? containerName, string blobName)
    {
        var container = new BlobContainerClient(_connectionStrings.AzureBlobStorage, containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);
        return blob;
    }

    private static string? GetFileFromUrl(string url, string container)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        var userDataContainerString = container + "/";
        var fileUrl =
            url[(url.IndexOf(userDataContainerString, StringComparison.Ordinal) + userDataContainerString.Length)..];
        return HttpUtility.UrlDecode(fileUrl);
    }
}