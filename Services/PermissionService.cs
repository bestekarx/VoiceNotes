

namespace VoiceNotes.Services
{
    public class PermissionService : IPermissionService
    {
        public async Task<bool> CheckMicrophonePermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckMicrophonePermission Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RequestMicrophonePermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RequestMicrophonePermission Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckStoragePermissionAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CheckStoragePermission Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RequestStoragePermissionAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RequestStoragePermission Error: {ex.Message}");
                return false;
            }
        }
    }
} 