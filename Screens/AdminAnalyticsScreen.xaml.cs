namespace LalabotApplication.Screens;

public partial class AdminAnalyticsScreen : ContentPage
{
    public AdminAnalyticsScreen(AdminAnalyticsScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is AdminAnalyticsScreenModel viewModel)
        {
            _ = viewModel.LoadAnalytics();
        }
    }
}