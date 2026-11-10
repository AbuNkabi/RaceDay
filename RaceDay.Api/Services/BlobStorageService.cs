using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public interface IBlobStorageService
{
    Task<string> UploadImageAsync(Stream content, string fileName, string contentType,
        CancellationToken cancellationToken = default);
    Task<(byte[] Bytes, string ContentType)?> DownloadByNameAsync(string? name,
        CancellationToken cancellationToken = default);
    Task DeleteByUrlAsync(string? name, CancellationToken cancellationToken = default);
}

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly IConfiguration _config;
    private readonly ILogger<BlobStorageService> _logger;
    public BlobStorageService(IConfiguration config, ILogger<BlobStorageService> logger)
    { _config = config; _logger = logger; }

    private async Task<BlobContainerClient> ContainerAsync(CancellationToken ct)
    {
        var connection = _config["AzureStorage:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Azure storage is not configured.");
        var name = _config["AzureStorage:ContainerName"] ?? "raceday-images";
        var client = new BlobContainerClient(connection, name);
        await client.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: ct);
        return client;
    }

    // Keep the container private; database stores opaque blob name, not a public URL.
    public async Task<string> UploadImageAsync(Stream content, string fileName,
        string contentType, CancellationToken cancellationToken = default)
    {
        var client = await ContainerAsync(cancellationToken);
        var name = $"{Guid.NewGuid():N}{Path.GetExtension(fileName).ToLowerInvariant()}";
        await client.GetBlobClient(name).UploadAsync(content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            cancellationToken);
        return name;
    }

    private static bool IsSafeName(string name) =>
        name.Length < 100 && !name.Contains('/') && !name.Contains('\\') && !name.Contains("..");

    public async Task<(byte[] Bytes, string ContentType)?> DownloadByNameAsync(string? name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || !IsSafeName(name)) return null;
        var client = await ContainerAsync(cancellationToken);
        var blob = client.GetBlobClient(name);
        if (!(await blob.ExistsAsync(cancellationToken)).Value) return null;
        var content = (await blob.DownloadContentAsync(cancellationToken)).Value;
        return (content.Content.ToArray(), content.Details.ContentType ?? "application/octet-stream");
    }

    public async Task DeleteByUrlAsync(string? name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || !IsSafeName(name)) return;
        try
        {
            var client = await ContainerAsync(cancellationToken);
            await client.GetBlobClient(name).DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not clean up old blob."); }
    }
}
