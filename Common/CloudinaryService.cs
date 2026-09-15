using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using SkillHive.Enums;

namespace SkillHive.Common
{
    public class CloudinaryUploadResult
    {
        public string Url { get; set; } = string.Empty;
        public string PublicId { get; set; } = string.Empty;
        public long Bytes { get; set; }
        public string? Format { get; set; }
    }

    public class CloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]
            );

            _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        }

        public async Task<CloudinaryUploadResult> UploadAsync(Stream stream, string fileName, string folder)
        {
            var fileType = FileHelper.GetFileTypeCategory(fileName);
            var fileDescription = new FileDescription(fileName, stream);

            UploadResult result;

            if (fileType == FileType.IMAGE)
            {
                result = await _cloudinary.UploadAsync(new ImageUploadParams
                {
                    File = fileDescription,
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                });
            }
            else if (fileType == FileType.VIDEO)
            {
                result = await _cloudinary.UploadAsync(new VideoUploadParams
                {
                    File = fileDescription,
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                });
            }
            else
            {
                result = await _cloudinary.UploadAsync(new RawUploadParams
                {
                    File = fileDescription,
                    Folder = folder,
                    UseFilename = true,
                    UniqueFilename = true,
                });
            }

            if (result.Error != null)
                throw new InvalidOperationException($"Cloudinary upload failed: {result.Error.Message}");

            return new CloudinaryUploadResult
            {
                Url = result.SecureUrl.ToString(),
                PublicId = result.PublicId,
                Bytes = result.Bytes,
                Format = result.Format,
            };
        }

        public async Task<bool> DeleteAsync(string publicId)
        {
            var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));
            return result.Result == "ok";
        }
    }
}