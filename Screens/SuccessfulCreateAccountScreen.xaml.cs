using System.Threading.Tasks;

namespace LalabotApplication.Screens;

public partial class SuccessfulCreateAccountScreen : ContentPage
{
    private int _secondsRemaining = 5;
    private bool _hasNavigated = false;

	public SuccessfulCreateAccountScreen()
	{
		InitializeComponent();
	}

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StartCountdown();
    }

    private async void StartCountdown()
    {
        _secondsRemaining = 5;
        _hasNavigated = false;

        while (_secondsRemaining > 0 && !_hasNavigated)
        {
            TimerLabel.Text = $"Redirecting to login in {_secondsRemaining} second{(_secondsRemaining != 1 ? "s" : "")}...";
            await Task.Delay(1000);
            _secondsRemaining--;
        }

        if (!_hasNavigated)
        {
            await NavigateToLogin();
        }
    }

    private async Task NavigateToLogin()
    {
        if (_hasNavigated) return;

        _hasNavigated = true;
        await Shell.Current.GoToAsync("///Login");
    }
    

    private async void NavigateLogin_Clicked(object sender, EventArgs e)
    {
        await NavigateToLogin();
    }

}