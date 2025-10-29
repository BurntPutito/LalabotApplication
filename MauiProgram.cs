using Firebase.Auth;
using Firebase.Auth.Providers;
using LalabotApplication.Screens;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Firebase.Database;

namespace LalabotApplication
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseSkiaSharp()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("FontAwesomeSolid.otf", "AwesomeSolid");
                    fonts.AddFont("boxicons-brands.ttf", "BoxIcons");
                    fonts.AddFont("Lineicons.ttf", "LineIcons");
                    fonts.AddFont("Outfit-VariableFont_wght.ttf", "OutfitVariable");
                    // Thin - 100, ExtraLight - 200, Light - 300, Regular - 400, Medium - 500, SemiBold - 600, Bold - 700, ExtraBold - 800, Black - 900
                    fonts.AddFont("Outfit-ExtraLight.ttf", "OutfitExtraLight");
                    fonts.AddFont("Outfit-Regular.ttf", "OutfitRegular");
                    fonts.AddFont("Outfit-Light.ttf", "OutfitLight");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            builder.Services.AddSingleton(new FirebaseAuthClient(new FirebaseAuthConfig()
            {
                ApiKey = "AIzaSyDbUDCax6orMurh6hSBKbKg51luC8xa1GQ",
                AuthDomain = "lalabotapplication.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
                {
                    new EmailProvider(),
                    new GoogleProvider(),
                    new FacebookProvider()
                }
            }));

            builder.Services.AddSingleton(new FirebaseClient("https://lalabotapplication-default-rtdb.asia-southeast1.firebasedatabase.app/"));
            builder.Services.AddSingleton<Login>();
            builder.Services.AddSingleton<LoginScreenModel>();
            builder.Services.AddSingleton<CreateAccountScreen>();
            builder.Services.AddSingleton<CreateAccountScreenModel>();

            builder.Services.AddTransient<HomeScreenModel>();
            builder.Services.AddTransient<HomeScreen>();
            builder.Services.AddTransient<SettingsScreenModel>();
            builder.Services.AddTransient<SettingsScreen>();
            builder.Services.AddTransient<CreateDeliveryScreenModel>();
            builder.Services.AddTransient<CreateDeliveryScreen>();

            return builder.Build();
        }
    }
}
