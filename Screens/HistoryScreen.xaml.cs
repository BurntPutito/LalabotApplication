namespace LalabotApplication.Screens;

public partial class HistoryScreen : ContentPage
{
    private readonly HistoryScreenModel _viewModel;

    public HistoryScreen(HistoryScreenModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Reload when returning to screen
        if (_viewModel != null)
        {
            _ = _viewModel.Refresh();
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Stop listening when leaving screen to save resources
        _viewModel?.StopListening();
    }
}