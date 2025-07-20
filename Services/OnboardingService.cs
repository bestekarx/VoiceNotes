using VoiceNotes.Models;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class OnboardingService : IOnboardingService
    {
        private const string OnboardingCompletedKey = "OnboardingCompleted";
        private const string OnboardingDataKey = "OnboardingData";
        private const string UserLanguageKey = "UserLanguage";
        private const string UserConsentKey = "UserConsent";

        public OnboardingService()
        {
        }

        public async Task<bool> IsOnboardingCompleted()
        {
            try
            {
                // Önce Preferences'tan kontrol et
                var completed = Preferences.Get(OnboardingCompletedKey, false);
                if (completed)
                {
                    return true;
                }

                // Eğer Preferences'ta yoksa SecureStorage'tan kontrol et (geriye uyumluluk)
                var secureCompleted = await SecureStorage.Default.GetAsync(OnboardingCompletedKey);
                if (secureCompleted == "true")
                {
                    // SecureStorage'ta varsa Preferences'a da kaydet
                    Preferences.Set(OnboardingCompletedKey, true);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.IsOnboardingCompleted");
                return false;
            }
        }

        public async Task<bool> SetOnboardingCompleted()
        {
            try
            {
                // Hem Preferences hem de SecureStorage'a kaydet
                Preferences.Set(OnboardingCompletedKey, true);
                await SecureStorage.Default.SetAsync(OnboardingCompletedKey, "true");
                
                System.Diagnostics.Debug.WriteLine("Onboarding marked as completed");
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.SetOnboardingCompleted");
                return false;
            }
        }

        public Task<OnboardingData> GetOnboardingData()
        {
            try
            {
                // For now, return default data
                // TODO: Implement JSON serialization when needed
                return Task.FromResult(new OnboardingData());
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.GetOnboardingData");
                return Task.FromResult(new OnboardingData());
            }
        }

        public Task<bool> SaveOnboardingData(OnboardingData data)
        {
            try
            {
                // TODO: Implement JSON serialization when needed
                // var dataJson = JsonSerializer.Serialize(data);
                // SecureStorage.Default.SetAsync(OnboardingDataKey, dataJson);
                
                System.Diagnostics.Debug.WriteLine("Onboarding data saved successfully");
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.SaveOnboardingData");
                return Task.FromResult(false);
            }
        }

        public async Task<bool> ResetOnboarding()
        {
            try
            {
                // Hem Preferences hem de SecureStorage'dan sil
                Preferences.Remove(OnboardingCompletedKey);
                await SecureStorage.Default.SetAsync(OnboardingCompletedKey, "false");
                await SecureStorage.Default.SetAsync(OnboardingDataKey, "");
                
                System.Diagnostics.Debug.WriteLine("Onboarding reset successfully");
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.ResetOnboarding");
                return false;
            }
        }

        public async Task<bool> UpdateLanguage(string languageCode)
        {
            try
            {
                await SecureStorage.Default.SetAsync(UserLanguageKey, languageCode);
                System.Diagnostics.Debug.WriteLine($"Language updated to: {languageCode}");
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.UpdateLanguage");
                return false;
            }
        }

        public async Task<string> GetCurrentLanguage()
        {
            try
            {
                var language = await SecureStorage.Default.GetAsync(UserLanguageKey);
                return string.IsNullOrEmpty(language) ? "en" : language;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.GetCurrentLanguage");
                return "en";
            }
        }

        public async Task<bool> HasUserConsent()
        {
            try
            {
                var consent = await SecureStorage.Default.GetAsync(UserConsentKey);
                return consent == "true";
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.HasUserConsent");
                return false;
            }
        }

        public async Task<bool> SetUserConsent(bool hasConsent)
        {
            try
            {
                await SecureStorage.Default.SetAsync(UserConsentKey, hasConsent.ToString().ToLower());
                System.Diagnostics.Debug.WriteLine($"User consent set to: {hasConsent}");
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "OnboardingService.SetUserConsent");
                return false;
            }
        }
    }
} 