using Roots_Wings.Services.Interfaces;

namespace Roots_Wings.Services
{
    public class ProfilePictureService : IProfilePictureService
    {
        private readonly IWebHostEnvironment _env;

        private const long MaxBytes = 3 * 1024 * 1024; // 3 MB
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private static readonly string[] AllowedContentTypes = { "image/jpeg", "image/png", "image/webp" };
        private const string FolderName = "images/profile-pictures";

        public ProfilePictureService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<ProfilePictureResult> SaveAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return ProfilePictureResult.None(); // no upload — valid

            if (file.Length > MaxBytes)
                return ProfilePictureResult.Fail("The image must be 3 MB or smaller.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext) ||
                !AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                return ProfilePictureResult.Fail("Only JPG, JPEG , PNG or WEBP images are allowed.");

            var folder = Path.Combine(_env.WebRootPath, "images", "profile-pictures");
            Directory.CreateDirectory(folder); // no-op if it exists

            // Sanitize the original name: strip path, keep it short, remove anything risky.
            var originalName = Path.GetFileNameWithoutExtension(file.FileName);
            var safeName = string.Concat(originalName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (string.IsNullOrEmpty(safeName)) safeName = "image";
            if (safeName.Length > 40) safeName = safeName.Substring(0, 40);

            // guid + - + name + ext   →   a1b2c3d4-...-vacation.png
            var fileName = $"{Guid.NewGuid()}-{safeName}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return ProfilePictureResult.Ok($"/{FolderName}/{fileName}");
        }

        public void Delete(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return;

            var fullPath = Path.Combine(
                _env.WebRootPath,
                relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}