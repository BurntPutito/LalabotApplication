using System.Text.RegularExpressions;

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

    bool _isPasswordFieldFocused = false;

    private void PasswordEntry_Focused(object sender, FocusEventArgs e)
    {
        _isPasswordFieldFocused = true;
    }

    private async void PasswordEntry_Unfocused(object sender, FocusEventArgs e)
    {
        _isPasswordFieldFocused = false;

        // Fade back to normal when not focused
        await PasswordIconBackground.ColorTo(
            (PasswordIconBackground.Fill as SolidColorBrush)?.Color ?? Colors.AliceBlue,
            Colors.AliceBlue,
            c => PasswordIconBackground.Fill = new SolidColorBrush(c),
            400,
            Easing.CubicInOut);
    }

    private async void OnPasswordTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isPasswordFieldFocused)
            return; // Don't animate colors if not focused

        string password = e.NewTextValue ?? string.Empty;
        int score = 0;

        if (password.Length >= 8) score++;
        if (password.Length >= 10) score++;
        if (Regex.IsMatch(password, @"\\d")) score++;
        if (Regex.IsMatch(password, @"[A-Z]")) score++;
        if (Regex.IsMatch(password, @"[!@#$%^&*(),.?{}|<>]")) score++;

        Color targetColor = Color.FromArgb("#FF8A80"); // weak

        if (score >= 4)
            targetColor = Color.FromArgb("#81C784"); // strong
        else if (score >= 2)
            targetColor = Color.FromArgb("#FFD54F"); // medium

        await PasswordIconBackground.ColorTo(
            (PasswordIconBackground.Fill as SolidColorBrush)?.Color ?? Colors.AliceBlue,
            targetColor,
            c => PasswordIconBackground.Fill = new SolidColorBrush(c),
            300,
            Easing.CubicInOut);
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

public static class AnimationExtensions
{
    public static Task<bool> ColorTo(this VisualElement self, Color fromColor, Color toColor, Action<Color> callback, uint length = 250, Easing easing = null)
    {
        easing ??= Easing.Linear;
        Color transform(double t) => Color.FromRgba(
            fromColor.Red + (toColor.Red - fromColor.Red) * t,
            fromColor.Green + (toColor.Green - fromColor.Green) * t,
            fromColor.Blue + (toColor.Blue - fromColor.Blue) * t,
            fromColor.Alpha + (toColor.Alpha - fromColor.Alpha) * t);

        var taskCompletionSource = new TaskCompletionSource<bool>();

        new Animation(v => callback(transform(v)), 0, 1)
            .Commit(self, "ColorTo", 16, length, easing, (v, c) => taskCompletionSource.SetResult(c));

        return taskCompletionSource.Task;
    }
}