using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Firebase.Auth;

namespace LalabotApplication.Screens
{
    public partial class LoginScreenModel : ObservableObject
    {
        private readonly FirebaseAuthClient _authClient;

        [ObservableProperty]
        private bool _rememberMe = false; // Default to false

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        public LoginScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        private async Task SaveCredentials()
        {
            try
            {
                await SecureStorage.SetAsync("saved_email", Email);
                await SecureStorage.SetAsync("saved_password", Password);
                await SecureStorage.SetAsync("remember_me", "true");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save credentials: {ex.Message}");
            }
        }

        private async Task ClearSavedCredentials()
        {
            try
            {
                SecureStorage.Remove("saved_email");
                SecureStorage.Remove("saved_password");
                SecureStorage.Remove("remember_me");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to clear credentials: {ex.Message}");
            }
        }

        public async Task LoadSavedCredentials()
        {
            try
            {
                var savedRememberMe = await SecureStorage.GetAsync("remember_me");
                if (savedRememberMe == "true")
                {
                    RememberMe = true;
                    Email = await SecureStorage.GetAsync("saved_email") ?? string.Empty;
                    Password = await SecureStorage.GetAsync("saved_password") ?? string.Empty;

                    // Auto-login if credentials exist
                    if (!string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password))
                    {
                        await Login();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load credentials: {ex.Message}");
            }
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
                _ => "Login failed. Please check your credentials and try again."
            };
        }

        private string GetForgotPasswordErrorMessage(Exception ex)
        {
            string errorMessage = ex.Message.ToLower();
            return errorMessage switch
            {
                var msg when msg.Contains("user not found") || msg.Contains("no user record") =>
                    "No account found with this email address.",
                var msg when msg.Contains("invalid email") || msg.Contains("badly formatted") =>
                    "Please enter a valid email address.",
                var msg when msg.Contains("network") =>
                    "Network error. Please check your internet connection.",
                var msg when msg.Contains("too many requests") =>
                    "Too many requests. Please try again later.",
                _ => "Failed to send reset email. Please try again."
            };
        }

        [RelayCommand]
        private async Task ForgotPassword()
        {
            //prompts user to enter their email if not yet entered
            if (string.IsNullOrWhiteSpace(Email))
            {
                string emailInput = await Shell.Current.DisplayPromptAsync(
                    "Reset Password",
                    "Please enter your email address:",
                    placeholder: "email@example.com",
                    keyboard: Keyboard.Email);

                if (string.IsNullOrWhiteSpace(emailInput))
                {
                    return;
                }

                Email = emailInput;

            }

            try
            {
                // send password reset email
                await _authClient.ResetEmailPasswordAsync(Email);

                await Shell.Current.DisplayAlert(
                    "Password Reset",
                    $"A password reset link has been sent to {Email}. Please check your email, it could also be in your spam folder.",
                    "OK");
            }
            catch (Exception ex)
            {
                string friendlyMessage = GetForgotPasswordErrorMessage(ex);
                await Shell.Current.DisplayAlert("Reset Failed", friendlyMessage, "OK");
            }
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
                    // Save credentials if Remember Me is enabled
                    if (RememberMe)
                    {
                        await SaveCredentials();
                    }
                    else
                    {
                        await ClearSavedCredentials();
                    }

                    await Shell.Current.GoToAsync("///MainPage");
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