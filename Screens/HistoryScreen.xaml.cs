namespace LalabotApplication.Screens;

public partial class HistoryScreen : ContentPage
{
	public HistoryScreen()
	{
		InitializeComponent();
	}

    private void OnButtonClicked(object sender, EventArgs e)
    {
        // Reset all buttons to default color
        DeliveredBtn.BackgroundColor = Colors.AliceBlue;
        DeliveredBtn.BorderColor = Color.FromRgba("#2D4A6E");
        DeliveredBtn.TextColor = Colors.Black;
        CancelledBtn.BackgroundColor = Colors.AliceBlue;
        CancelledBtn.BorderColor = Color.FromRgba("#2D4A6E");
        CancelledBtn.TextColor = Colors.Black;
        PendingBtn.BackgroundColor = Colors.AliceBlue;
        PendingBtn.BorderColor = Color.FromRgba("#2D4A6E");
        PendingBtn.TextColor = Colors.Black;
        // Highlight the clicked one
        var clickedButton = (Button)sender;
        clickedButton.TextColor = Colors.White;
        clickedButton.BackgroundColor = Color.FromRgba("#2D4A6E");
    }

}