using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace RaceDayApi.Services;

public class BlobStorageService : IBlobStorageService
{
    private static readonly TimeSpan SasLifetime = TimeSpan.FromHours(1);
    private readonly BlobContainerClient? _container;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var o = options.Value;
        if (!string.IsNullOrWhiteSpace(o.ConnectionString))
        {
            _container = new BlobContainerClient(o.ConnectionString, o.ContainerName);
        }
    }

    public bool IsConfigured => _container is not null;

    public async Task<string> UploadImageAsync(Stream content, string contentType, string folder, CancellationToken ct = default)
    {
        if (_container is null) throw new InvalidOperationException("Blob storage is not configured.");

        // Private container: images are served through short-lived SAS URLs (see GetReadUrl).
        await _container.CreateIfNotExistsAsync(cancellationToken: ct);

        var extension = contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };

        var blob = _container.GetBlobClient($"{folder}/{Guid.NewGuid():N}{extension}");
        await blob.UploadAsync(
            content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            ct);

        return blob.Uri.ToString();
    }

    public string? GetReadUrl(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) || _container is null) return storedUrl;
        if (!TryGetBlobName(storedUrl, out var blobName)) return storedUrl;

        var blob = _container.GetBlobClient(blobName);
        if (!blob.CanGenerateSasUri) return storedUrl;

        return blob.GenerateSasUri(BlobSasPermissions.Read, DateTimeOffset.UtcNow.Add(SasLifetime)).ToString();
    }

    public async Task DeleteAsync(string? storedUrl, CancellationToken ct = default)
    {
        if (_container is null || !TryGetBlobName(storedUrl, out var blobName)) return;

        try
        {
            await _container.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: ct);
        }
        catch
        {
            // Best effort: a failed cleanup must never fail the user's request.
        }
    }

    private bool TryGetBlobName(string? storedUrl, out string blobName)
    {
        blobName = string.Empty;
        if (_container is null || string.IsNullOrWhiteSpace(storedUrl)) return false;
        if (!Uri.TryCreate(storedUrl, UriKind.Absolute, out var uri)) return false;

        // Blob URLs look like https://<account>.blob.core.windows.net/<container>/<blob name>
        // (Azurite adds the account name as the first path segment, so search for the container).
        var marker = $"/{_container.Name}/";
        var path = uri.AbsolutePath;
        var index = path.IndexOf(marker, StringComparison.Ordinal);
        if (index < 0) return false;

        blobName = Uri.UnescapeDataString(path[(index + marker.Length)..]);
        return blobName.Length > 0;
    }
}
