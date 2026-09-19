using DaMaiDeparte.Web.Resources;
using Microsoft.Extensions.Options;

namespace DaMaiDeparte.Web.Services;

/// <summary>Stores donation images on local disk under wwwroot.</summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = ".jpg",
        [".jpeg"] = ".jpg",
        [".png"] = ".png",
        [".webp"] = ".webp"
    };

    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/pjpeg", "image/png", "image/webp"
    };

    private readonly IWebHostEnvironment _environment;
    private readonly FileStorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(
        IWebHostEnvironment environment,
        IOptions<FileStorageOptions> options,
        ILogger<LocalFileStorageService> logger)
    {
        _environment = environment;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string?> ValidateImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length <= 0)
        {
            return UiText.Validation.ImageInvalid;
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            return UiText.Validation.ImageTooLarge;
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.ContainsKey(extension)
            || !AllowedContentTypes.Contains(file.ContentType))
        {
            return UiText.Validation.ImageFormat;
        }

        // Check the file signature so a renamed file cannot pass as an image.
        var header = new byte[12];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
            if (read < header.Length || DetectExtension(header) is null)
            {
                return UiText.Validation.ImageFormat;
            }
        }

        return null;
    }

    public async Task<string> SaveImageAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var header = new byte[12];
        await using (var probe = file.OpenReadStream())
        {
            await probe.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        }

        // The stored extension is derived from the content, never from the user-supplied name.
        var extension = DetectExtension(header)
            ?? throw new InvalidOperationException("The file was not validated before saving.");

        var folder = _options.UploadFolder.Trim('/').Replace('\\', '/');
        var physicalFolder = Path.Combine(_environment.WebRootPath, folder.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(physicalFolder);

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(physicalFolder, fileName);

        try
        {
            await using var target = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await using var source = file.OpenReadStream();
            await source.CopyToAsync(target, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image upload failed while writing file {FileName}", fileName);
            TryDelete(physicalPath);
            throw;
        }

        return $"{folder}/{fileName}";
    }

    public void DeleteImage(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return;
        }

        var folder = _options.UploadFolder.Trim('/').Replace('\\', '/');
        var normalized = relativePath.TrimStart('/').Replace('\\', '/');

        // Only delete files inside the upload folder.
        if (!normalized.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase) || normalized.Contains(".."))
        {
            return;
        }

        TryDelete(Path.Combine(_environment.WebRootPath, normalized.Replace('/', Path.DirectorySeparatorChar)));
    }

    private void TryDelete(string physicalPath)
    {
        try
        {
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not delete image file");
        }
    }

    internal static string? DetectExtension(ReadOnlySpan<byte> header)
    {
        if (header.Length >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF)
        {
            return ".jpg";
        }

        ReadOnlySpan<byte> pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (header.Length >= 8 && header[..8].SequenceEqual(pngSignature))
        {
            return ".png";
        }

        // RIFF....WEBP
        if (header.Length >= 12
            && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50)
        {
            return ".webp";
        }

        return null;
    }
}
