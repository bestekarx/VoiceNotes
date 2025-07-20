using System.Threading.Tasks;

namespace VoiceNotes.Services
{
    public interface IAuthenticationService
    {
        Task<bool> GoogleLoginAsync();
        Task<bool> AppleLoginAsync();
        Task LogoutAsync();
        bool IsLoggedIn { get; }
    }
}