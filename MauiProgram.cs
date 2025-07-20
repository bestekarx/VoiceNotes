using Microsoft.Extensions.Logging;
using VoiceNotes.Data;
using VoiceNotes.ViewModels;
using VoiceNotes.Views;
using VoiceNotes.Services;
using Refit;
using System.Net.Http.Headers;
using Syncfusion.Maui.Core.Hosting;
using Syncfusion.Licensing;

namespace VoiceNotes;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Syncfusion lisans anahtarını ayarla
        SyncfusionLicenseProvider.RegisterLicense("Mzk1ODExOUAzMzMwMmUzMDJlMzAzYjMzMzAzYm4wSFNZa2lhU2VUSDJ6SUszVkVrU2gxME1rVHJWeVRFNHNKVXEvQXJUYzA9");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .ConfigureSyncfusionCore(); // Syncfusion Core'u ekle

        // Database
        builder.Services.AddSingleton<NoteDatabase>(provider =>
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db");
            return new NoteDatabase(path);
        });

        // Services
        builder.Services.AddSingleton<IAudioRecordingService, AudioRecordingService>();
        builder.Services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();
        builder.Services.AddSingleton<IOnboardingService, OnboardingService>();
        builder.Services.AddSingleton<IUserService, UserService>();
        builder.Services.AddSingleton<IPermissionService, PermissionService>();
        builder.Services.AddSingleton<IAuthenticationService, AuthenticationService>();
        builder.Services.AddSingleton<IPremiumService, PremiumService>();
        builder.Services.AddSingleton<IUsageTrackingService, UsageTrackingService>();
        builder.Services.AddSingleton<IInAppPurchaseService, InAppPurchaseService>();
        
        // Backend API base URL (canlı Render sunucusu)
        var apiBaseUrl = "https://voicenotesapi.onrender.com";

        builder.Services.AddRefitClient<IApiService>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(apiBaseUrl);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        builder.Services.AddSingleton<IFileUploadService, FileUploadService>();

        // ViewModels
        builder.Services.AddTransient<NoteListViewModel>();
        builder.Services.AddTransient<NoteDetailViewModel>();
        builder.Services.AddTransient<WelcomeViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddTransient<NoteListPage>();
        builder.Services.AddTransient<NoteDetailPage>();
        builder.Services.AddTransient<WelcomePage>();
        builder.Services.AddTransient<SettingsPage>();

        builder.Services.AddLogging(configure => 
        {
            configure.AddDebug();
        });

        return builder.Build();
    }
}
