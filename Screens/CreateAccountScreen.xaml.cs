namespace LalabotApplication.Screens;

public partial class CreateAccountScreen : ContentPage
{
    public CreateAccountScreen(CreateAccountScreenModel screenModel)
    {
        InitializeComponent();

        // Add tap gesture recognizer to the root layout
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) =>
        {
            // Unfocus all entries when tapped outside
            NameEntry?.Unfocus();
            EmailEntry?.Unfocus();
            PasswordEntry?.Unfocus();
        };

        // Attach gesture to the whole page
        if (this.Content is Layout layout)
        {
            layout.GestureRecognizers.Add(tapGesture);
        }

        BindingContext = screenModel;

    }

    private bool _isPasswordVisible = false;
    private void TogglePasswordVisibility(object sender, EventArgs e)
    {
        _isPasswordVisible = !_isPasswordVisible;
        PasswordEntry.IsPassword = !_isPasswordVisible;

        if (_isPasswordVisible)
        {
            ((ImageButton)sender).Source = "eye_closed.png";
        }
        else
        {
            ((ImageButton)sender).Source = "eye_big.png";
        }
    }

}