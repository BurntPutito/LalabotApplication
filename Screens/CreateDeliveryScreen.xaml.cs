namespace LalabotApplication.Screens;

public partial class CreateDeliveryScreen : ContentPage
{
	public CreateDeliveryScreen(CreateDeliveryScreenModel viewModel)
	{
		InitializeComponent();
        BindingContext = viewModel;
    }
}