using RaceDayApi.Services;

namespace RaceDayApi.Tests;

/// <summary>
/// Stand-in for Azure Blob Storage so tests (and CI) never need an Azure account.
/// </summary>
public class FakeBlobStorageService : IBlobStorageService
{
    public bool IsConfigured => true;

    public Task<string> UploadImageAsync(Stream content, string contentType, string folder, CancellationToken ct = default)
        => Task.FromResult($"https://fake.blob.local/raceday-images/{folder}/{Guid.NewGuid():N}.png");

    public string? GetReadUrl(string? storedUrl)
        => string.IsNullOrWhiteSpace(storedUrl) ? storedUrl : storedUrl + "?sas=fake";

    public Task DeleteAsync(string? storedUrl, CancellationToken ct = default)
        => Task.CompletedTask;
}
