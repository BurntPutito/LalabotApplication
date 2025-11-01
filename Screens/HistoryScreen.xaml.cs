namespace LalabotApplication.Screens;

public partial class HistoryScreen : ContentPage
{
    public HistoryScreen(HistoryScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}