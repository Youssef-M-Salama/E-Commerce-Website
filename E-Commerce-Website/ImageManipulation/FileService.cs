namespace E_Commerce_Website;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

#region Service
public class FileService : IFileService
{
    private readonly IWebHostEnvironment _environment;

    public FileService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    // ===============================
    // Helper Methods
    // ===============================

    private static string BuildWebPath(params string[] segments)
    {
        return "/" + string.Join("/", segments);
    }

    private string BuildPhysicalPath(params string[] segments)
    {
        return Path.Combine(segments);
    }

    private static long ConvertMBToBytes(int sizeInMB)
    {
        return sizeInMB * 1024L * 1024L;
    }

    // ===============================
    // Save File
    // ===============================

    public async Task<ImageSaveResult> SaveFileAsync(
        IFormFile imageFile,
        string[] allowedFileExtensions,
        string folderName,
        int maxFileSizeInMB = 5)
    {
        if (imageFile == null)
        {
            return new ImageSaveResult
            {
                IsSaved = false,
                Message = "The image file is null."
            };
        }

        //  File size validation
        var maxSizeInBytes = ConvertMBToBytes(maxFileSizeInMB);

        if (imageFile.Length > maxSizeInBytes)
        {
            return new ImageSaveResult
            {
                IsSaved = false,
                Message = $"The image size exceeds the maximum allowed size of {maxFileSizeInMB} MB."
            };
        }

        var uploadFolderPath = BuildPhysicalPath(
            _environment.WebRootPath,
            folderName
        );

        if (!Directory.Exists(uploadFolderPath))
        {
            Directory.CreateDirectory(uploadFolderPath);
        }

        var extension = Path.GetExtension(imageFile.FileName);

        if (!allowedFileExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return new ImageSaveResult
            {
                IsSaved = false,
                Message = $"Only the following extensions are allowed: {string.Join(", ", allowedFileExtensions)}."
            };
        }

        var fileName = $"{Guid.NewGuid()}{extension}";
        var physicalPath = BuildPhysicalPath(
            uploadFolderPath,
            fileName
        );

        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await imageFile.CopyToAsync(stream);

        var relativePath = BuildWebPath(folderName, fileName);

        return new ImageSaveResult
        {
            FilePath = relativePath,
            IsSaved = true,
            Message = "The image was saved successfully."
        };
    }

    // ===============================
    // Delete File
    // ===============================

    public ImageDeleteResult DeleteFile(string relativeFilePath)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return new ImageDeleteResult
            {
                IsDeleted = false,
                Message = "The file path is null or empty, so the file cannot be deleted."
            };
        }

        var segments = new[]
        {
            _environment.WebRootPath
        }
        .Concat(
            relativeFilePath
                .TrimStart('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
        )
        .ToArray();

        var physicalPath = BuildPhysicalPath(segments);

        if (!File.Exists(physicalPath))
        {
            return new ImageDeleteResult
            {
                IsDeleted = false,
                Message = "The file path does not exist."
            };
        }

        File.Delete(physicalPath);

        return new ImageDeleteResult
        {
            IsDeleted = true,
            Message = "The image was deleted successfully."
        };
    }
}

#endregion

