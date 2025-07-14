namespace VoiceNotes.Services
{
    public interface IAudioPlaybackService
    {
        bool IsPlaying { get; }
        string CurrentPlayingFile { get; }
        
        event EventHandler<bool>? PlaybackStatusChanged;
        event EventHandler<TimeSpan>? PositionChanged;
        event EventHandler<string>? CurrentFileChanged;
        
        Task<bool> PlayAsync(string filePath);
        Task StopAsync();
        Task<TimeSpan> GetDurationAsync(string filePath);
        bool IsPlayingFile(string filePath);
    }
} 