namespace LalabotApplication.Screens;

public partial class AvatarPickerScreen : ContentPage
{
    public AvatarPickerScreen(AvatarPickerScreenModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}