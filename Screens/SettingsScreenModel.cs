using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using LalabotApplication.Models;
using LalabotApplication.Services;

namespace LalabotApplication.Screens
{
    public partial class SettingsScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        private readonly FirebaseClient _firebaseDb;

        [ObservableProperty]
        private string _username = "User";

        [ObservableProperty]
        private string _avatarSource = "avatar_0.png";

        public SettingsScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _ = LoadUserProfile(); // Load profile when screen loads
        }

        [RelayCommand]
        private async Task Logout()
        {
            bool confirm = await Shell.Current.DisplayAlert(
                "Logout",
                "Are you sure you want to logout?",
                "Yes",
                "No");

            if (confirm)
            {
                try
                {
                    // Sign out from Firebase
                    _authClient.SignOut();

                    // Navigate to Login screen
                    await Shell.Current.GoToAsync("///Login");
                }
                catch (Exception ex)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to logout. Please try again.", "OK");
                }
            }
        }

        public async Task LoadUserProfile()
        {
            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    var userProfile = await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .OnceSingleAsync<UserProfile>();

                    if (userProfile != null)
                    {
                        Username = userProfile.Username ?? "User";
                        AvatarSource = AvatarHelper.GetAvatarSource(userProfile.ProfileAvatarIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Username = "User";
                AvatarSource = "avatar_0.png";
            }
        }

        [RelayCommand]
        private async Task NavigateToProfile()
        {
            await Shell.Current.GoToAsync("///ProfileScreen");
        }
    }
}