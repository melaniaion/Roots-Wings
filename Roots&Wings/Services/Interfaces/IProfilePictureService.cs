namespace Roots_Wings.Services.Interfaces
{
    public class ProfilePictureResult
    {
        public bool Success { get; init; }
        public string? Path { get; init; }         // relative web path or null
        public string? ErrorMessage { get; init; } // null when Success

        public static ProfilePictureResult Ok(string path) =>
            new() { Success = true, Path = path };

        public static ProfilePictureResult Fail(string message) =>
            new() { Success = false, ErrorMessage = message };

        public static ProfilePictureResult None() =>
            new() { Success = true, Path = null };  // no file uploaded = valid
    }

    public interface IProfilePictureService
    {
        Task<ProfilePictureResult> SaveAsync(IFormFile file);
        void Delete(string relativePath);
    }
}