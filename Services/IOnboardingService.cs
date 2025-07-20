using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IOnboardingService
    {
        Task<bool> IsOnboardingCompleted();
        Task<bool> SetOnboardingCompleted();
        Task<OnboardingData> GetOnboardingData();
        Task<bool> SaveOnboardingData(OnboardingData data);
        Task<bool> ResetOnboarding();
        Task<bool> UpdateLanguage(string languageCode);
        Task<string> GetCurrentLanguage();
        Task<bool> HasUserConsent();
        Task<bool> SetUserConsent(bool hasConsent);
    }
} 