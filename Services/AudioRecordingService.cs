using Plugin.AudioRecorder;
using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public class AudioRecordingService : IAudioRecordingService
    {
        private readonly AudioRecorderService _audioRecorderService;
        private bool _isRecording;
        private DateTime _recordingStartTime;
        private string _currentFilePath = string.Empty;
        private int _currentNoteId;
        private Timer? _recordingTimer;
        
        // Security limits
        private static readonly TimeSpan MaxRecordingDuration = TimeSpan.FromHours(1); // 1 hour limit
        private static readonly long MaxFileSizeBytes = 50 * 1024 * 1024; // 50MB limit

        public AudioRecordingService()
        {
            _audioRecorderService = new AudioRecorderService();
        }

        public bool IsRecording => _isRecording;
        public TimeSpan CurrentRecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
        public TimeSpan RemainingTime => MaxRecordingDuration - CurrentRecordingDuration;

        public event EventHandler<bool>? RecordingStatusChanged;
        public event EventHandler<double>? AudioLevelChanged;
        public event EventHandler<TimeSpan>? RecordingDurationChanged;
        public event EventHandler<string>? RecordingLimitReached;

        public async Task<bool> StartRecordingAsync()
        {
            try
            {
                if (_isRecording) return false;

                _recordingStartTime = DateTime.Now;
                var fileName = $"recording_{_recordingStartTime:yyyyMMdd_HHmmss}.wav";
                _currentFilePath = Path.Combine(FileSystem.Current.AppDataDirectory, fileName);
                
                // Platform permissions
                var hasPermission = await CheckPermissionsAsync();
                if (!hasPermission)
                    return false;

                await _audioRecorderService.StartRecording();
                _isRecording = true;
                
                // Start security timer for 1-hour limit
                StartRecordingTimer();
                
                RecordingStatusChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Start Recording Error: {ex.Message}");
                return false;
            }
        }

        private void StartRecordingTimer()
        {
            // Timer that fires every 30 seconds to check duration and update UI
            _recordingTimer = new Timer(async _ => 
            {
                if (!_isRecording) return;
                
                var currentDuration = DateTime.Now - _recordingStartTime;
                
                // Update duration for UI
                RecordingDurationChanged?.Invoke(this, currentDuration);
                
                // Check if we've hit the 1-hour limit
                if (currentDuration >= MaxRecordingDuration)
                {
                    await StopRecordingDueToLimit();
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        private async Task StopRecordingDueToLimit()
        {
            try
            {
                if (_isRecording)
                {
                    await StopRecordingAsync();
                    RecordingLimitReached?.Invoke(this, "Maksimum kayıt süresi (1 saat) dolmuştur. Kayıt otomatik olarak durduruldu.");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stop Recording Due To Limit Error: {ex.Message}");
            }
        }

        public async Task<string> StopRecordingAsync()
        {
            try
            {
                if (!_isRecording) return string.Empty;

                // Stop and dispose timer
                _recordingTimer?.Dispose();
                _recordingTimer = null;

                await _audioRecorderService.StopRecording();
                _currentFilePath = _audioRecorderService.GetAudioFilePath();
                _isRecording = false;
                RecordingStatusChanged?.Invoke(this, false);
                
                return _currentFilePath;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Stop Recording Error: {ex.Message}");
                return string.Empty;
            }
        }

        public async Task<AudioRecord> CreateAudioRecordAsync(int noteId)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
                {
                    throw new InvalidOperationException("No recording file found");
                }

                var recordingEndTime = DateTime.Now;
                var duration = recordingEndTime - _recordingStartTime;
                var fileInfo = new FileInfo(_currentFilePath);
                
                var audioRecord = new AudioRecord
                {
                    NoteID = noteId,
                    Title = $"Recording {recordingEndTime:MMM dd, HH:mm}",
                    FileName = Path.GetFileName(_currentFilePath),
                    FilePath = _currentFilePath,
                    Duration = duration,
                    FileSizeBytes = fileInfo.Length,
                    RecordedAt = _recordingStartTime,
                    HasSummary = false,
                    SummaryText = string.Empty,
                    IsUploaded = false
                };

                return audioRecord;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Create AudioRecord Error: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> CheckPermissionsAsync()
        {
            try
            {
                var status = await Permissions.RequestAsync<Permissions.Microphone>();
                return status == PermissionStatus.Granted;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Permission Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteAudioFileAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete Audio File Error: {ex.Message}");
                return false;
            }
        }

        public async Task<TimeSpan> GetAudioDurationAsync(string filePath)
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
                const int bytesPerSecond = 88200; // 44100 Hz * 16-bit * 1 channel
                
                // Subtract WAV header (typically 44 bytes)
                var audioDataBytes = Math.Max(0, fileSizeBytes - 44);
                var durationSeconds = (double)audioDataBytes / bytesPerSecond;
                
                return TimeSpan.FromSeconds(Math.Max(1, durationSeconds)); // Minimum 1 second
            }
            catch
            {
                return TimeSpan.FromSeconds(10); // Fallback duration
            }
        }
    }
} 