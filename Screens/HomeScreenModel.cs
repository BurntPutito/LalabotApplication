using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class HomeScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private string _username = "User";

        [ObservableProperty]
        private string _email = string.Empty;

        public HomeScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
            LoadUserInfo();
        }

        private async void LoadUserInfo()
        {
            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    // Get display name (username) if it was set
                    Username = user.Info?.DisplayName ?? "User";

                    // Get email
                    Email = user.Info?.Email ?? "";
                }
            }
            catch (Exception ex)
            {
                // Handle error
                Username = "User";
            }
        }
    }
}