using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IAudioRecordingService
    {
        bool IsRecording { get; }
        TimeSpan CurrentRecordingDuration { get; }
        TimeSpan RemainingTime { get; }
        
        event EventHandler<bool>? RecordingStatusChanged;
        event EventHandler<double>? AudioLevelChanged;
        event EventHandler<TimeSpan>? RecordingDurationChanged;
        event EventHandler<string>? RecordingLimitReached;
        
        Task<bool> StartRecordingAsync();
        Task<string> StopRecordingAsync();
        Task<AudioRecord> CreateAudioRecordAsync(int noteId);
        Task<bool> CheckPermissionsAsync();
        Task<bool> DeleteAudioFileAsync(string filePath);
        Task<TimeSpan> GetAudioDurationAsync(string filePath);
    }
} 