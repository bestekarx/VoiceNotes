using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IFileUploadService
    {
        Task<bool> UploadAudioRecordAsync(AudioRecord audioRecord);
        Task<bool> UploadMultipleAudioRecordsAsync(List<AudioRecord> audioRecords);
        Task<List<AudioRecord>> GetUnuploadedAudioRecordsAsync();
        Task<bool> SyncUnuploadedFilesAsync();
    }
} 