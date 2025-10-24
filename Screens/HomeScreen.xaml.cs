namespace LalabotApplication.Screens;

public partial class HomeScreen : ContentPage
{
    public HomeScreen(HomeScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}