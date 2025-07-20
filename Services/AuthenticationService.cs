using System.Threading.Tasks;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        public bool IsLoggedIn => Preferences.Get("IsLoggedIn", false);

        public async Task<bool> GoogleLoginAsync()
        {
            try
            {
                // Simulate Google login
                await Task.Delay(1000);
                Preferences.Set("IsLoggedIn", true);
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "AuthenticationService.GoogleLoginAsync");
                return false;
            }
        }

        public async Task<bool> AppleLoginAsync()
        {
            try
            {
                // Simulate Apple login
                await Task.Delay(1000);
                Preferences.Set("IsLoggedIn", true);
                return true;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "AuthenticationService.AppleLoginAsync");
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                Preferences.Set("IsLoggedIn", false);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "AuthenticationService.LogoutAsync");
            }
        }
    }
}