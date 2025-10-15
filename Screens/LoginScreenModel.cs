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
        private string _email;

        [ObservableProperty]
        private string _password;

        public LoginScreenModel(FirebaseAuthClient authClient)
        {
            _authClient = authClient;
        }

        [RelayCommand]
        private async Task Login()
        {
            try 
            {
                await _authClient.SignInWithEmailAndPasswordAsync(Email, Password);
                await Shell.Current.GoToAsync("///MainPage");
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Login Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task NavigateCreateAccount()
        {
            await Shell.Current.GoToAsync("///CreateAccountScreen");
        }
    }
}
