using Plugin.AudioRecorder;

namespace VoiceNotes.Services
{
    public class AudioPlaybackService : IAudioPlaybackService
    {
        private readonly AudioPlayer _audioPlayer;
        private bool _isPlaying;
        private string _currentPlayingFile = string.Empty;

        public AudioPlaybackService()
        {
            _audioPlayer = new AudioPlayer();
        }

        public bool IsPlaying => _isPlaying;
        public string CurrentPlayingFile => _currentPlayingFile;

        public event EventHandler<bool>? PlaybackStatusChanged;
        public event EventHandler<TimeSpan>? PositionChanged;
        public event EventHandler<string>? CurrentFileChanged;

        public async Task<bool> PlayAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return false;
                }

                // Stop current playback if playing different file
                if (_isPlaying && _currentPlayingFile != filePath)
                {
                    await StopAsync();
                }

                // Toggle pause/play if same file
                if (_currentPlayingFile == filePath && _isPlaying)
                {
                    await StopAsync();
                    return true;
                }

                _currentPlayingFile = filePath;
                _audioPlayer.Play(filePath);
                _isPlaying = true;
                
                PlaybackStatusChanged?.Invoke(this, true);
                CurrentFileChanged?.Invoke(this, filePath);
                
                // Get estimated duration and auto-stop after that time
                var estimatedDuration = await GetDurationAsync(filePath);
                if (estimatedDuration > TimeSpan.Zero)
                {
                    _ = Task.Run(async () =>
                    {
                        // Wait for the estimated duration + small buffer
                        var waitTime = estimatedDuration.Add(TimeSpan.FromSeconds(1));
                        await Task.Delay(waitTime);
                        
                        // If still playing the same file, auto-stop
                        if (_isPlaying && _currentPlayingFile == filePath)
                        {
                            await StopAsync();
                        }
                    });
                }
                else
                {
                    // Fallback: stop after 30 seconds for safety
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(TimeSpan.FromSeconds(30));
                        if (_isPlaying && _currentPlayingFile == filePath)
                        {
                            await StopAsync();
                        }
                    });
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Play Error: {ex.Message}");
                return false;
            }
        }

        public async Task StopAsync()
        {
            try
            {
                // Try to stop the audio player - Plugin.AudioRecorder might use different methods
                try
                {
                    _audioPlayer?.Pause();
                }
                catch (Exception playerEx)
                {
                    System.Diagnostics.Debug.WriteLine($"AudioPlayer Pause Error: {playerEx.Message}");
                }
                
                _isPlaying = false;
                var previousFile = _currentPlayingFile;
                _currentPlayingFile = string.Empty;
                
                PlaybackStatusChanged?.Invoke(this, false);
                CurrentFileChanged?.Invoke(this, string.Empty);
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stop Error: {ex.Message}");
            }
        }

        public async Task<TimeSpan> GetDurationAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    return TimeSpan.Zero;
                }

                // Get file info
                var fileInfo = new FileInfo(filePath);
                var fileSizeBytes = fileInfo.Length;
                
                // Improved calculation for audio duration
                // For WAV files: Sample Rate = 44100 Hz, Bits per Sample = 16, Channels = 1 (mono)
                // Bytes per second = Sample Rate × (Bits per Sample / 8) × Channels
                // = 44100 × 2 × 1 = 88,200 bytes per second for mono
                // = 44100 × 2 × 2 = 176,400 bytes per second for stereo
                
                // Assume mono recording (most voice recordings are mono)
                const int bytesPerSecond = 88200; // 44100 Hz * 16-bit * 1 channel
                
                // Subtract WAV header (typically 44 bytes)
                var audioDataBytes = Math.Max(0, fileSizeBytes - 44);
                var durationSeconds = (double)audioDataBytes / bytesPerSecond;
                
                return TimeSpan.FromSeconds(Math.Max(1, durationSeconds)); // Minimum 1 second
            }
            catch
            {
                // Fallback duration
                return TimeSpan.FromSeconds(10);
            }
        }

        public bool IsPlayingFile(string filePath)
        {
            return _isPlaying && _currentPlayingFile == filePath;
        }
    }
} 