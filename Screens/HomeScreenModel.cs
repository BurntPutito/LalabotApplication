using CommunityToolkit.Mvvm.ComponentModel;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class HomeScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;
        private readonly FirebaseClient _firebaseDb;

        [ObservableProperty]
        private string _username = "User";

        public HomeScreenModel(FirebaseAuthClient authClient, FirebaseClient firebaseDb)
        {
            _authClient = authClient;
            _firebaseDb = firebaseDb;
            _ = LoadUserInfo(); // Load username when screen opens
        }

        private async Task LoadUserInfo()
        {
            try
            {
                var user = _authClient.User;

                if (user != null)
                {
                    // Get username from Firebase Database
                    var userData = await _firebaseDb
                        .Child("users")
                        .Child(user.Uid)
                        .Child("Username")
                        .OnceSingleAsync<string>();

                    if (!string.IsNullOrEmpty(userData))
                    {
                        Username = userData;
                    }
                }
            }
            catch (Exception ex)
            {
                // If error, keep default "User"
                Username = "Kupal";
            }
        }
    }
}