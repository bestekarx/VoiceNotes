using System.Threading.Tasks;
using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IUserService
    {
        Task<UserProfile> GetCurrentUserProfileAsync();
        Task<int> SaveUserProfileAsync(UserProfile profile);
        Task<UserPreferences> GetCurrentUserPreferencesAsync();
        Task<int> SaveUserPreferencesAsync(UserPreferences preferences);
    }
}