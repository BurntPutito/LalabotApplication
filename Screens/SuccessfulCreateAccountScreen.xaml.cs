using System.Threading.Tasks;

namespace LalabotApplication.Screens;

public partial class SuccessfulCreateAccountScreen : ContentPage
{
	public SuccessfulCreateAccountScreen()
	{
		InitializeComponent();
	}

    

    private async void NavigateLogin_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("///Login");
    }
}