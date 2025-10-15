namespace LalabotApplication.Screens;

public partial class Login : ContentPage
{
	public Login(LoginScreenModel screenModel)
	{
		InitializeComponent();

		BindingContext = screenModel;
	}

	private async void CreateAccount_Clicked(object sender, EventArgs e)
	{
        // You must provide a CreateAccountScreenModel instance as required by the constructor
        var screenModel = new CreateAccountScreenModel();
        await Navigation.PushAsync(new CreateAccountScreen(screenModel));
	}
}