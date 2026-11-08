namespace RaceDayApi.Services;

public interface IBlobStorageService
{
    /// <summary>True when a storage connection string has been configured.</summary>
    bool IsConfigured { get; }

    /// <summary>Uploads an image and returns the permanent blob URL (no access token).</summary>
    Task<string> UploadImageAsync(Stream content, string contentType, string folder, CancellationToken ct = default);

    /// <summary>
    /// Turns a stored blob URL into a URL a browser can load (adds a short-lived read-only
    /// SAS token, so the container can stay private). Returns the input unchanged if it
    /// is null, empty or storage is not configured.
    /// </summary>
    string? GetReadUrl(string? storedUrl);

    /// <summary>Best-effort delete of a previously uploaded blob.</summary>
    Task DeleteAsync(string? storedUrl, CancellationToken ct = default);
}
