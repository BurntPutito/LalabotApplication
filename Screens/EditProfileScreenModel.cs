using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LalabotApplication.Models;
using LalabotApplication.Services;

namespace LalabotApplication.Screens
{
    [QueryProperty(nameof(SelectedAvatarIndex), "SelectedAvatarIndex")]
    [QueryProperty(nameof(SelectedAvatarUrl), "SelectedAvatarUrl")]
    public partial class EditProfileScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        [ObservableProperty]
        private string _username = "User";

        [ObservableProperty]
        private string _email = "user@email.com";

        [ObservableProperty]
        private string _avatarSource = "avatar_0.png";

        [ObservableProperty]
        private int _profileAvatarIndex = 0;

        [ObservableProperty]
        private string _customAvatarUrl = string.Empty;

        private string _originalUsername = string.Empty;
        private int _originalAvatarIndex = 0;
        private string _originalCustomAvatarUrl = string.Empty;
        private bool _hasLoadedData = false;

        public int SelectedAvatarIndex
        {
            set
            {
                // This is called when returning from AvatarPickerScreen
                ProfileAvatarIndex = value;

                // If -1, it means custom photo was selected
                if (value == -1)
                {
                    // CustomAvatarUrl will be set separately via SelectedAvatarUrl property
                    return;
                }

                // Clear custom URL when selecting default avatar
                CustomAvatarUrl = string.Empty;
                AvatarSource = AvatarHelper.GetAvatarSource(value);
            }
        }

        public string SelectedAvatarUrl
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    CustomAvatarUrl = value;
                    AvatarSource = value;
                    ProfileAvatarIndex = -1; // Custom photo indicator
                }
            }
        }

        public EditProfileScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
        }

        public async Task LoadProfileData()
        {
            // Only load once to prevent overwriting selected avatar
            if (_hasLoadedData)
                return;

            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    // Get user profile from Firebase
                    var userProfile = await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .OnceSingleAsync<UserProfile>();

                    if (userProfile != null)
                    {
                        Username = userProfile.Username ?? "User";
                        Email = userProfile.Email ?? user.Info.Email ?? "No email";
                        ProfileAvatarIndex = userProfile.ProfileAvatarIndex;
                        CustomAvatarUrl = userProfile.CustomAvatarUrl ?? string.Empty;

                        // Set avatar source (custom URL takes priority)
                        AvatarSource = AvatarHelper.GetAvatarSource(ProfileAvatarIndex, CustomAvatarUrl);

                        // Store original values
                        _originalUsername = Username;
                        _originalAvatarIndex = ProfileAvatarIndex;
                        _originalCustomAvatarUrl = CustomAvatarUrl;

                        _hasLoadedData = true;
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load profile data.", "OK");
            }
        }

        [RelayCommand]
        private async Task ChangeAvatar()
        {
            // Pass current avatar info to the picker
            var navigationParameter = new Dictionary<string, object>
            {
                { "CurrentAvatarIndex", ProfileAvatarIndex },
                { "CurrentAvatarUrl", CustomAvatarUrl ?? string.Empty }
            };

            // Use relative navigation
            await Shell.Current.GoToAsync(nameof(AvatarPickerScreen), navigationParameter);
        }

        [RelayCommand]
        private async Task SaveChanges()
        {
            // Validate username
            if (string.IsNullOrWhiteSpace(Username))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Username cannot be empty.", "OK");
                return;
            }

            if (Username.Length < 3)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Username must be at least 3 characters.", "OK");
                return;
            }

            // Check if anything changed
            bool usernameChanged = Username != _originalUsername;
            bool avatarChanged = (ProfileAvatarIndex != _originalAvatarIndex) ||
                                (CustomAvatarUrl != _originalCustomAvatarUrl);

            if (!usernameChanged && !avatarChanged)
            {
                await Shell.Current.DisplayAlert("No Changes", "No changes were made to your profile.", "OK");
                return;
            }

            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    // Update profile in Firebase
                    await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .PutAsync(new
                        {
                            Username = Username,
                            Email = Email,
                            ProfileAvatarIndex = ProfileAvatarIndex,
                            CustomAvatarUrl = CustomAvatarUrl ?? string.Empty
                        });

                    await Shell.Current.DisplayAlert("Success", "Profile updated successfully!", "OK");

                    // Reset flag so data reloads next time
                    _hasLoadedData = false;

                    // Navigate back to profile screen
                    await Shell.Current.GoToAsync("///ProfileScreen");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Failed to update profile: {ex.Message}", "OK");
            }
        }

        [RelayCommand]
        private async Task Cancel()
        {
            // Check if there are unsaved changes
            bool hasChanges = (Username != _originalUsername) ||
                             (ProfileAvatarIndex != _originalAvatarIndex) ||
                             (CustomAvatarUrl != _originalCustomAvatarUrl);

            if (hasChanges)
            {
                bool confirm = await Shell.Current.DisplayAlert(
                    "Unsaved Changes",
                    "You have unsaved changes. Are you sure you want to cancel?",
                    "Yes",
                    "No");

                if (!confirm)
                    return;
            }

            // Reset flag
            _hasLoadedData = false;

            await Shell.Current.GoToAsync("///ProfileScreen");
        }
    }
}