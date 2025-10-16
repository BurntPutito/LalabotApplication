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
    public partial class LoginScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        public LoginScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        private string GetUserFriendlyLoginErrorMessage(Exception ex)
        {
            string errorMessage = ex.Message.ToLower();

            return errorMessage switch
            {
                var msg when msg.Contains("user not found") || msg.Contains("no user record") =>
                    "No account found with this email. Please check your email or create a new account.",
                var msg when msg.Contains("missingpassword") || msg.Contains("invalid_login_credentials") =>
                    "Incorrect password. Please try again.",
                var msg when msg.Contains("invalid email address") || msg.Contains("badly formatted") =>
                    "Please enter a valid email address.",
                var msg when msg.Contains("user disabled") =>
                    "This account has been disabled. Please contact support.",
                var msg when msg.Contains("too many requests") || msg.Contains("too many failed") =>
                    "Too many failed login attempts. Please try again later.",
                var msg when msg.Contains("network") =>
                    "Network error. Please check your internet connection.",
                var msg when msg.Contains("invalid credential") || msg.Contains("invalid login") =>
                    "Invalid email or password. Please try again.",
                //_ => "Login failed. Please check your credentials and try again."
            };
        }

        [RelayCommand]
        private async Task Login()
        {
            // Validate fields
            if (string.IsNullOrWhiteSpace(Email))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter your email address.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter your password.", "OK");
                return;
            }

            try
            {
                var result = await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);

                if (result != null && !string.IsNullOrEmpty(result.User?.Uid))
                {
                    await Shell.Current.GoToAsync("///MainPage");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Login Error", "Login failed. Please try again.", "OK");
                }
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetUserFriendlyLoginErrorMessage(ex);
                await Shell.Current.DisplayAlert("Login Failed", friendlyMessage, "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateCreateAccount()
        {
            await Shell.Current.GoToAsync("///CreateAccountScreen");
        }
    }
}