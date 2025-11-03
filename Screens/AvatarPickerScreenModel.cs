namespace LalabotApplication.Screens;

public class AvatarPickerScreenModel : ContentPage
{
	public AvatarPickerScreenModel()
	{
		Content = new VerticalStackLayout
		{
			Children = {
				new Label { HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center, Text = "Welcome to .NET MAUI!"
				}
			}
		};
	}
}