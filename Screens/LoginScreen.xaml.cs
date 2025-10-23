using System.Threading.Tasks;

namespace LalabotApplication.Screens;

public partial class Login : ContentPage
{
	public Login(LoginScreenModel screenModel)
	{
		InitializeComponent();

		BindingContext = screenModel;
	}

	private bool _isPasswordVisible = false;
    private void TogglePasswordVisibility(object sender, EventArgs e)
    {
		_isPasswordVisible = !_isPasswordVisible;
		PasswordEntry.IsPassword = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            ((ImageButton)sender).Source = "eye_closed.png";
        }
        else
        {
            ((ImageButton)sender).Source = "eye_big.png";
        }
    }
}