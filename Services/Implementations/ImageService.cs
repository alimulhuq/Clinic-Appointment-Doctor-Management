using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Clinic_Application_Doctor_Management.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace Clinic_Application_Doctor_Management.Services.Implementations
{
    public class ImageService : IImageService
    {
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;   // 5 MB
        private const int MaxDimension = 800;                     // px
        private static readonly string[] AllowedExtensions =
            { ".jpg", ".jpeg", ".png", ".webp", ".gif" };

        private readonly IWebHostEnvironment _env;

        public ImageService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<string?> SaveDoctorImageAsync(IFormFile file, int doctorId)
        {
            if (file == null || file.Length == 0) return null;

            if (file.Length > MaxFileSizeBytes)
                throw new InvalidOperationException("Image is too large. Maximum allowed size is 5 MB.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!AllowedExtensions.Contains(ext))
                throw new InvalidOperationException("Unsupported file type. Use JPG, PNG, WEBP, or GIF.");

            var folder = Path.Combine(_env.WebRootPath, "uploads", "doctors");
            Directory.CreateDirectory(folder);

            var fileName = $"doc_{doctorId}_{Guid.NewGuid():N}{ext}";
            var fullPath = Path.Combine(folder, fileName);

            using var stream = file.OpenReadStream();
            using var image = await Image.LoadAsync(stream);

            if (image.Width > MaxDimension || image.Height > MaxDimension)
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(MaxDimension, MaxDimension)
                }));
            }

            var saveExt = ext == ".png" ? ".png" : ".jpg";
            var saveName = Path.ChangeExtension(fileName, saveExt);
            var savePath = Path.Combine(folder, saveName);

            if (saveExt == ".png")
                await image.SaveAsPngAsync(savePath);
            else
                await image.SaveAsJpegAsync(savePath, new JpegEncoder { Quality = 85 });

            if (!string.Equals(fullPath, savePath, StringComparison.OrdinalIgnoreCase))
                TryDelete(fullPath);

            return $"/uploads/doctors/{saveName}";
        }

        public void DeleteImage(string? relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl)) return;
            if (!relativeUrl.StartsWith("/uploads/doctors/", StringComparison.OrdinalIgnoreCase)) return;

            var relative = relativeUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relative);
            TryDelete(fullPath);
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* swallow */ }
        }
    }
}