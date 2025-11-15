namespace LalabotApplication.Screens;

public partial class HomeScreen : ContentPage
{
    private readonly HomeScreenModel _viewModel;

    public HomeScreen(HomeScreenModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload when returning to screen
        _ = _viewModel.Refresh();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop listening when leaving screen to save resources
        _viewModel.StopListening();
    }
}