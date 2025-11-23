using SkiaSharp;
using System.Text.Json;

namespace LalabotApplication.Services
{
    public class ImageUploadService
    {
        private const string IMGBB_API_KEY = "d4eaa3cd6e9c5b0bdae0e132466b2c42";
        private const string IMGBB_UPLOAD_URL = "https://api.imgbb.com/1/upload";
        private readonly HttpClient _httpClient;

        public ImageUploadService()
        {
            _httpClient = new HttpClient();
        }

        // Upload image and return the URL
        public async Task<string> UploadImageAsync(byte[] imageData)
        {
            try
            {
                // Convert to base64
                string base64Image = Convert.ToBase64String(imageData);

                // Create form data
                var formData = new MultipartFormDataContent();
                formData.Add(new StringContent(IMGBB_API_KEY), "key");
                formData.Add(new StringContent(base64Image), "image");

                // Upload to ImgBB
                var response = await _httpClient.PostAsync(IMGBB_UPLOAD_URL, formData);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ImgBBResponse>(responseContent);

                    // Debug: Check what URL we got
                    System.Diagnostics.Debug.WriteLine($"Upload response: {responseContent}");
                    System.Diagnostics.Debug.WriteLine($"Image URL: {result?.data?.url}");

                    return result?.data?.url ?? string.Empty;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Upload error: {ex.Message}");
                return string.Empty;
            }
        }

        // Resize and compress image using SkiaSharp
        public byte[] ResizeAndCompressImage(Stream imageStream, int targetSize = 512, int quality = 85)
        {
            using var inputStream = new SKManagedStream(imageStream);
            using var original = SKBitmap.Decode(inputStream);

            if (original == null)
                throw new Exception("Failed to decode image");

            // Calculate new dimensions (square crop from center)
            int cropSize = Math.Min(original.Width, original.Height);
            int offsetX = (original.Width - cropSize) / 2;
            int offsetY = (original.Height - cropSize) / 2;

            // Crop to square
            using var cropped = new SKBitmap(cropSize, cropSize);
            using var canvas = new SKCanvas(cropped);
            var sourceRect = new SKRect(offsetX, offsetY, offsetX + cropSize, offsetY + cropSize);
            var destRect = new SKRect(0, 0, cropSize, cropSize);
            canvas.DrawBitmap(original, sourceRect, destRect);

            // Resize to target size
            using var resized = cropped.Resize(new SKImageInfo(targetSize, targetSize), SKFilterQuality.High);

            // Compress to JPEG with quality setting
            using var image = SKImage.FromBitmap(resized);
            using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);

            var result = data.ToArray();

            // If still over 500KB, reduce quality and try again
            if (result.Length > 500 * 1024 && quality > 50)
            {
                using var imageRetry = SKImage.FromBitmap(resized);
                using var dataRetry = imageRetry.Encode(SKEncodedImageFormat.Jpeg, quality - 20);
                return dataRetry.ToArray();
            }

            return result;
        }
    }


    // ImgBB API response models
    public class ImgBBResponse
    {
        public ImgBBData data { get; set; }
    }

    public class ImgBBData
    {
        public string url { get; set; }
        public string display_url { get; set; }
    }
}