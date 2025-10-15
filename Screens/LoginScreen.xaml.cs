using System.Threading.Tasks;

namespace LalabotApplication.Screens;

public partial class Login : ContentPage
{
	public Login(LoginScreenModel screenModel)
	{
		InitializeComponent();

		BindingContext = screenModel;
	}
}