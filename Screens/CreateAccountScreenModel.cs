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

        [RelayCommand]
        private async Task CreateAccount()
        {
            try
            {
                await _authClient.CreateUserWithEmailAndPasswordAsync(Email, Password, Username);
                await Shell.Current.GoToAsync("///Login");
            }
            catch (Exception ex)
            {
                // Handle any potential errors during account creation
                await Shell.Current.DisplayAlert("Error", ex.Message, "OK");
            }
        }
    }
}
