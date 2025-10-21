﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;
using Firebase.Auth.Providers;
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
        private string _email = string.Empty;

        [ObservableProperty]
        private string _username = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

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
        
        /*
        // Placeholder for Google Sign-In implementation
        [RelayCommand]
        private async Task SignInWithGoogle()
        {
            try
            {
                var credential = await _authClient.SignInWithGoogle();

                if (credential != null && !string.IsNullOrEmpty(credential.User?.Uid))
                {
                    await Shell.Current.DisplayAlert("Success", "Signed in with Google successfully!", "OK");
                    await Shell.Current.GoToAsync("///MainPage");
                }
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetUserFriendlyErrorMessage(ex);
                await Shell.Current.DisplayAlert("Google Sign-In Failed", friendlyMessage, "OK");
            }
        }

        // Placeholder for Facebook Sign-In implementation
        [RelayCommand]
        private async Task SignInWithFacebook()
        {
            try
            {
                var credential = await _authClient.SignInWithFacebook();

                if (credential != null && !string.IsNullOrEmpty(credential.User?.Uid))
                {
                    await Shell.Current.DisplayAlert("Success", "Signed in with Facebook successfully!", "OK");
                    await Shell.Current.GoToAsync("///MainPage");
                }
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetUserFriendlyErrorMessage(ex);
                await Shell.Current.DisplayAlert("Facebook Sign-In Failed", friendlyMessage, "OK");
            }
        }*/

        [RelayCommand]
        private async Task CreateAccount()
        {
            // Validate username before creating account
            if (string.IsNullOrWhiteSpace(Username))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter a username.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Email))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter an email address.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter a password.", "OK");
                return;
            }

            if (Password.Length < 12)
            {
                await Shell.Current.DisplayAlert("Validation Error", "Please enter at least a minimum of 12 characters.", "OK");
                return;
            }

            try
            {
                // Create account with email and password only
                var result = await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password);

                if (result != null && !string.IsNullOrEmpty(result.User?.Uid))
                {
                    await Shell.Current.GoToAsync("///SuccessfulCreateAccountScreen");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to create account. Please try again.", "OK");
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