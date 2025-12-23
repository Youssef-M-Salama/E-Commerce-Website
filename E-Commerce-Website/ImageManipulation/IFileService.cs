namespace E_Commerce_Website;

using Microsoft.AspNetCore.Http;

public interface IFileService
{
    Task<ImageSaveResult> SaveFileAsync(
        IFormFile imageFile,
        string[] allowedFileExtensions,
        string folderName,
        int maxFileSizeInMB);

    ImageDeleteResult DeleteFile(string relativeFilePath);
}

