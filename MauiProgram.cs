using Microsoft.Extensions.Logging;
using VoiceNotes.Data;
using VoiceNotes.ViewModels;
using VoiceNotes.Views;
using VoiceNotes.Services;
using Refit;
using System.Net.Http.Headers;

namespace VoiceNotes;

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
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Database
        builder.Services.AddSingleton<NoteDatabase>(provider =>
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "notes.db");
            return new NoteDatabase(path);
        });

        // Services
        builder.Services.AddSingleton<IAudioRecordingService, AudioRecordingService>();
        builder.Services.AddSingleton<IAudioPlaybackService, AudioPlaybackService>();
        // Backend API base URL (canlı Render sunucusu)
        var apiBaseUrl = "https://voicenotesapi.onrender.com";

        builder.Services.AddRefitClient<IApiService>()
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(apiBaseUrl);
                c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                // c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "<token>"); // Gerekirse
            });
        builder.Services.AddSingleton<IFileUploadService, FileUploadService>();

        // ViewModels
        builder.Services.AddTransient<NoteListViewModel>();
        builder.Services.AddTransient<NoteDetailViewModel>();

        // Views
        builder.Services.AddTransient<NoteListPage>();
        builder.Services.AddTransient<NoteDetailPage>();

        builder.Services.AddLogging(configure => configure.AddDebug());

        return builder.Build();
    }
}
