namespace LalabotApplication
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();

            // Register routes for modal navigation
            Routing.RegisterRoute(nameof(Screens.AvatarPickerScreen), typeof(Screens.AvatarPickerScreen));
        }
    }
}
