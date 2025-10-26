using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class SettingsScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        public SettingsScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
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
    }
}