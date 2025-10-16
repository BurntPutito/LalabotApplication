using Firebase.Auth;
using Firebase.Auth.Providers;
using LalabotApplication.Screens;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;

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
                    new EmailProvider()
                }
            }));

            builder.Services.AddSingleton<Login>();
            builder.Services.AddSingleton<LoginScreenModel>();
            builder.Services.AddSingleton<CreateAccountScreen>();
            builder.Services.AddSingleton<CreateAccountScreenModel>();

            return builder.Build();
        }
    }
}
