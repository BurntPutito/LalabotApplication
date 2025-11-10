using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LalabotApplication.Models;
using LalabotApplication.Services;

namespace LalabotApplication.Screens
{
    public partial class ProfileScreenModel : ObservableObject
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

        public ProfileScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
        }

        public async Task LoadProfileData()
        {
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

                        // Update avatar source
                        AvatarSource = AvatarHelper.GetAvatarSource(ProfileAvatarIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", "Failed to load profile data.", "OK");
            }
        }

        [RelayCommand]
        private async Task EditProfile()
        {
            await Shell.Current.GoToAsync("///EditProfileScreen");
        }

        [RelayCommand]
        private async Task TestAvatarPicker()
        {
            await Shell.Current.GoToAsync("///AvatarPickerScreen");
        }
    }
}