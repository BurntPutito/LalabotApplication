using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LalabotApplication.Screens
{
    public partial class CreateAccountScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private string _email;

        [ObservableProperty]
        private string _username;

        [ObservableProperty]
        private string _password;

        public CreateAccountScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        private string GetUserFriendlyErrorMessage(Exception ex)
        {
            string errorMessage = ex.Message.ToLower();

            return errorMessage switch
            {
                var msg when msg.Contains("email") && msg.Contains("already") =>
                    "This email is already registered. Please use a different email or try logging in.",
                var msg when msg.Contains("password") && msg.Contains("weak") =>
                    "Password is too weak. Please use at least 6 characters.",
                var msg when msg.Contains("invalid email") || msg.Contains("badly formatted") =>
                    "Please enter a valid email address.",
                var msg when msg.Contains("network") =>
                    "Network error. Please check your internet connection.",
                var msg when msg.Contains("too many requests") =>
                    "Too many attempts. Please try again later.",
                _ => "Failed to create account. Please try again."
            };
        }

        [RelayCommand]
        private async Task CreateAccount()
        {
            try
            {
                var result = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);
                
                if(result != null && !string.IsNullOrEmpty(result.User?.Uid))
                {
                   await Shell.Current.GoToAsync("///Login"); 
                }
                
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetUserFriendlyErrorMessage(ex);
                await Shell.Current.DisplayAlert("Account Creation Failed", friendlyMessage, "OK");
            }
        }
    }
}
