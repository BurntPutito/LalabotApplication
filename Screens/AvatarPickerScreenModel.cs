using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LalabotApplication.Services;

namespace LalabotApplication.Screens
{
    [QueryProperty(nameof(CurrentAvatarIndex), "CurrentAvatarIndex")]
    [QueryProperty(nameof(CurrentAvatarUrl), "CurrentAvatarUrl")]
    public partial class AvatarPickerScreenModel : ObservableObject
    {
        private readonly ImageUploadService _imageUploadService;

        [ObservableProperty]
        private int _selectedAvatarIndex = 0;

        [ObservableProperty]
        private string _selectedAvatarUrl = string.Empty;

        [ObservableProperty]
        private bool _isUploading = false;

        private int _initialAvatarIndex = 0;
        private string _initialAvatarUrl = string.Empty;

        // Visual feedback properties
        public bool HasCustomPhoto => !string.IsNullOrEmpty(SelectedAvatarUrl);
        public bool HasNoCustomPhoto => !HasCustomPhoto;

        public string UploadButtonTitle => HasCustomPhoto ? "Custom Photo Selected" : "Upload Custom Photo";
        public string UploadButtonSubtitle => HasCustomPhoto ? "Tap to change photo" : "Take a photo or choose from gallery";

        public Color CustomPhotoStrokeColor => HasCustomPhoto ? Color.FromArgb("#2D4A6E") : Color.FromArgb("#E0E0E0");
        public int CustomPhotoStrokeThickness => HasCustomPhoto ? 4 : 2;

        public AvatarPickerScreenModel()
        {
            _imageUploadService = new ImageUploadService();
        }

        public int CurrentAvatarIndex
        {
            set
            {
                _initialAvatarIndex = value;
                SelectedAvatarIndex = value;
            }
        }

        public string CurrentAvatarUrl
        {
            set
            {
                _initialAvatarUrl = value;
                SelectedAvatarUrl = value;
                UpdateVisualFeedback();
            }
        }

        public int GetCurrentAvatarIndex() => _initialAvatarIndex;
        public string GetCurrentAvatarUrl() => _initialAvatarUrl;

        public void SetSelectedIndex(int index)
        {
            SelectedAvatarIndex = index;
            SelectedAvatarUrl = string.Empty; // Clear custom URL when selecting default
            UpdateVisualFeedback();
        }

        partial void OnSelectedAvatarUrlChanged(string value)
        {
            UpdateVisualFeedback();
        }

        private void UpdateVisualFeedback()
        {
            OnPropertyChanged(nameof(HasCustomPhoto));
            OnPropertyChanged(nameof(HasNoCustomPhoto));
            OnPropertyChanged(nameof(UploadButtonTitle));
            OnPropertyChanged(nameof(UploadButtonSubtitle));
            OnPropertyChanged(nameof(CustomPhotoStrokeColor));
            OnPropertyChanged(nameof(CustomPhotoStrokeThickness));
        }

        [RelayCommand]
        private async Task UploadPhoto()
        {
            try
            {
                // Ask user: Camera or Gallery?
                string action = await Shell.Current.DisplayActionSheet(
                    "Choose Photo Source",
                    "Cancel",
                    null,
                    "📷 Take Photo",
                    "🖼️ Choose from Gallery");

                if (action == "Cancel" || string.IsNullOrEmpty(action))
                    return;

                FileResult photo = null;

                if (action == "📷 Take Photo")
                {
                    // Check camera permission
                    var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                        if (cameraStatus != PermissionStatus.Granted)
                        {
                            await Shell.Current.DisplayAlert("Permission Denied", "Camera access is required to take photos.", "OK");
                            return;
                        }
                    }

                    photo = await MediaPicker.CapturePhotoAsync(new MediaPickerOptions
                    {
                        Title = "Take a profile photo"
                    });
                }
                else if (action == "🖼️ Choose from Gallery")
                {
                    // Check photos permission
                    var photosStatus = await Permissions.CheckStatusAsync<Permissions.Photos>();
                    if (photosStatus != PermissionStatus.Granted)
                    {
                        photosStatus = await Permissions.RequestAsync<Permissions.Photos>();
                        if (photosStatus != PermissionStatus.Granted)
                        {
                            await Shell.Current.DisplayAlert("Permission Denied", "Gallery access is required to choose photos.", "OK");
                            return;
                        }
                    }

                    photo = await MediaPicker.PickPhotoAsync(new MediaPickerOptions
                    {
                        Title = "Select a profile photo"
                    });
                }

                if (photo == null)
                    return;

                IsUploading = true;

                // Read and process the image
                using var stream = await photo.OpenReadAsync();
                byte[] imageData = _imageUploadService.ResizeAndCompressImage(stream);

                // Upload to ImgBB
                string imageUrl = await _imageUploadService.UploadImageAsync(imageData);

                IsUploading = false;

                if (string.IsNullOrEmpty(imageUrl))
                {
                    await Shell.Current.DisplayAlert("Upload Failed", "Failed to upload image. Please try again.", "OK");
                    return;
                }

                // Debug: Show the URL
                System.Diagnostics.Debug.WriteLine($"Uploaded image URL: {imageUrl}");

                // Set the custom avatar URL
                SelectedAvatarUrl = imageUrl;
                SelectedAvatarIndex = -1; // -1 indicates custom photo

                // Force the avatar source to update
                OnPropertyChanged(nameof(SelectedAvatarUrl));

                UpdateVisualFeedback();

                await Shell.Current.DisplayAlert("Success!", "Your photo has been uploaded! Tap 'Select Avatar' to confirm.", "OK");
            }
            catch (PermissionException)
            {
                await Shell.Current.DisplayAlert("Permission Required", "Please grant permission to access camera/photos in your device settings.", "OK");
                IsUploading = false;
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to upload photo: {ex.Message}", "OK");
                IsUploading = false;
            }
        }

        [RelayCommand]
        private async Task ConfirmSelection()
        {
            var navigationParameter = new Dictionary<string, object>
            {
                { "SelectedAvatarIndex", SelectedAvatarIndex },
                { "SelectedAvatarUrl", SelectedAvatarUrl ?? string.Empty }
            };

            await Shell.Current.GoToAsync("..", navigationParameter);
        }

        [RelayCommand]
        private async Task Cancel()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}