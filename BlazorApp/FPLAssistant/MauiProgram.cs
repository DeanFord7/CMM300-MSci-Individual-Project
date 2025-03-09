using FPLAssistant.Components.Dialog;
using FPLAssistant.Repositories;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Blazored.LocalStorage;
using FPLAssistant.Services;

namespace FPLAssistant
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddMudServices();
            builder.Services.AddScoped(sp => new HttpClient());
            builder.Services.AddScoped<PythonRepository>();
            builder.Services.AddScoped<FPLRepository>();
            builder.Services.AddScoped<DialogService>();
            builder.Services.AddBlazoredLocalStorage();
            builder.Services.AddScoped<TeamStateService>();


#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
