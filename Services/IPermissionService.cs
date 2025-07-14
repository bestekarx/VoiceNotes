namespace VoiceNotes.Services
{
    public interface IPermissionService
    {
        Task<bool> CheckMicrophonePermissionAsync();
        Task<bool> RequestMicrophonePermissionAsync();
        Task<bool> CheckStoragePermissionAsync();
        Task<bool> RequestStoragePermissionAsync();
    }
} 