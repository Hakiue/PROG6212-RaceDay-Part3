namespace RaceDayApi.Services;

/// <summary>
/// Bound from the "BlobStorage" configuration section. Leave ConnectionString empty
/// to run without image storage (upload endpoints then return 503).
/// Never commit a real connection string: use user-secrets or an environment variable
/// (BlobStorage__ConnectionString).
/// </summary>
public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "raceday-images";
}
