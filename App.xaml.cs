using VoiceNotes.Services;
using Microsoft.Extensions.Logging;
using VoiceNotes.Helpers;

namespace VoiceNotes;

public partial class App : Application
{
    private readonly IOnboardingService _onboardingService;
    private readonly ILogger<App> _logger;

    public App(IOnboardingService onboardingService, ILogger<App> logger)
    {
        InitializeComponent();
        _onboardingService = onboardingService;
        _logger = logger;
    }

    protected override async void OnStart()
    {
        base.OnStart();
        
        try
        {
            // Check if onboarding is completed
            var isOnboardingCompleted = await _onboardingService.IsOnboardingCompleted();
            
            if (!isOnboardingCompleted)
            {
                // Navigate to onboarding
                await Shell.Current.GoToAsync("///WelcomePage");
            }
            else
            {
                // Navigate to main app
                await Shell.Current.GoToAsync("///NoteListPage");
            }
        }
        catch (Exception ex)
        {
            VoiceCrashLogger.LogError(ex, "App.OnStart");
            // Fallback to main app
            await Shell.Current.GoToAsync("///NoteListPage");
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell()) { Title = "Voice Notes" };
    }
}