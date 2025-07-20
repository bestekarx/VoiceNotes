using VoiceNotes.Data;
using VoiceNotes.Models;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class UserService : IUserService
    {
        private readonly NoteDatabase _database;

        public UserService(NoteDatabase database)
        {
            _database = database;
        }

        public async Task<UserProfile> GetCurrentUserProfileAsync()
        {
            try
            {
                var profile = await _database.GetUserProfileAsync();
                return profile ?? new UserProfile();
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "UserService.GetCurrentUserProfileAsync");
                return new UserProfile();
            }
        }

        public async Task<int> SaveUserProfileAsync(UserProfile profile)
        {
            try
            {
                return await _database.SaveUserProfileAsync(profile);
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "UserService.SaveUserProfileAsync");
                return 0;
            }
        }

        public async Task<UserPreferences> GetCurrentUserPreferencesAsync()
        {
            try
            {
                var preferences = await _database.GetUserPreferencesAsync();
                return preferences ?? new UserPreferences();
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "UserService.GetCurrentUserPreferencesAsync");
                return new UserPreferences();
            }
        }

        public async Task<int> SaveUserPreferencesAsync(UserPreferences preferences)
        {
            try
            {
                return await _database.SaveUserPreferencesAsync(preferences);
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "UserService.SaveUserPreferencesAsync");
                return 0;
            }
        }
    }
}