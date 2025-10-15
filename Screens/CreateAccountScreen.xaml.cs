namespace LalabotApplication.Screens;

public partial class CreateAccountScreen : ContentPage
{
	public CreateAccountScreen(CreateAccountScreenModel screenModel)
	{
		InitializeComponent();

		BindingContext = screenModel;
	}
}