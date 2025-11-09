namespace LalabotApplication.Screens;

public partial class ProfileScreen : ContentPage
{
    public ProfileScreen(ProfileScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Reload profile data when screen appears
        if (BindingContext is ProfileScreenModel viewModel)
        {
            _ = viewModel.LoadProfileData();
        }
    }
}