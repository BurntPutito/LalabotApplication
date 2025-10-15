using LalabotApplication.Screens;

namespace LalabotApplication
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Wrap the NavigationPage in a Window to match the expected return type
            return new Window(new NavigationPage(new Login()));
        }
    }
}