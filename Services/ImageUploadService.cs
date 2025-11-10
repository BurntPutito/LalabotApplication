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

        // Resize and compress image to meet size limit (500KB)
        public async Task<byte[]> ResizeAndCompressImage(Stream imageStream, int maxWidth = 512, int maxHeight = 512)
        {
            try
            {
                // Load the image
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                // Use MAUI's built-in image loading
                var imageSource = ImageSource.FromStream(() => new MemoryStream(imageData));

                // For now, return original if under 500KB
                if (imageData.Length <= 500 * 1024) // 500KB
                {
                    return imageData;
                }

                // If over 500KB, we'll compress it
                // MAUI doesn't have built-in compression, so we'll reduce quality
                // by re-encoding with lower quality

                return await CompressImageData(imageData, maxWidth, maxHeight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Resize error: {ex.Message}");
                throw;
            }
        }

        private async Task<byte[]> CompressImageData(byte[] imageData, int maxWidth, int maxHeight)
        {
            // This is a simplified version
            // In production, you'd use SkiaSharp or ImageSharp for better compression

            // For now, if image is too large, we'll just return it
            // and let ImgBB handle the compression
            return imageData;
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