namespace LalabotApplication.Screens;

public partial class EditProfileScreen : ContentPage
{
    public EditProfileScreen(EditProfileScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Load profile data when screen appears
        if (BindingContext is EditProfileScreenModel viewModel)
        {
            _ = viewModel.LoadProfileData();
        }
    }
}