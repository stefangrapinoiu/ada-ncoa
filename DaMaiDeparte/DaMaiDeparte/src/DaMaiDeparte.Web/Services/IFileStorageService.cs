namespace DaMaiDeparte.Web.Services;

public interface IFileStorageService
{
    /// <summary>Returns a Romanian validation message if the file is not acceptable; otherwise null.</summary>
    Task<string?> ValidateImageAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Saves a validated image and returns its path relative to wwwroot.</summary>
    Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously saved image. Never throws.</summary>
    void DeleteImage(string? relativePath);
}

public sealed class FileStorageOptions
{
    public string UploadFolder { get; set; } = "uploads/donations";

    public long MaxFileSizeBytes { get; set; } = 5 * 1024 * 1024;
}
