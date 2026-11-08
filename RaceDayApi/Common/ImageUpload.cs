namespace RaceDayApi.Common;

public static class ImageUpload
{
    public const long MaxBytes = 5 * 1024 * 1024;

    private static readonly string[] AllowedContentTypes =
        { "image/jpeg", "image/png", "image/webp", "image/gif" };

    /// <summary>Returns an error message, or null when the file is acceptable.</summary>
    public static string? Validate(IFormFile? file)
    {
        if (file is null || file.Length == 0) return "Please choose an image file to upload.";
        if (file.Length > MaxBytes) return "The image must be 5 MB or smaller.";
        if (!AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return "Only JPEG, PNG, WebP or GIF images are allowed.";
        return null;
    }
}
