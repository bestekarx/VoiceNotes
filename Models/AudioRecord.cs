using SQLite;
using System.ComponentModel;

namespace VoiceNotes.Models
{
    public class AudioRecord : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        
        [Indexed]
        public int NoteID { get; set; }
        
        public string Title 
        { 
            get => _title; 
            set 
            { 
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            } 
        }
        
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime RecordedAt { get; set; }
        public bool HasSummary
        {
            get => _hasSummary;
            set
            {
                if (_hasSummary != value)
                {
                    _hasSummary = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanSummarize));
                }
            }
        }
        private bool _hasSummary;
        public string SummaryText { get; set; } = string.Empty;
        public bool IsUploaded { get; set; }
        private bool _isSummaryExpanded;
        public bool IsSummaryExpanded
        {
            get => _isSummaryExpanded;
            set
            {
                if (_isSummaryExpanded != value)
                {
                    _isSummaryExpanded = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _isEditingSummary;
        public bool IsEditingSummary
        {
            get => _isEditingSummary;
            set
            {
                if (_isEditingSummary != value)
                {
                    _isEditingSummary = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _isEditingTranscript;
        public bool IsEditingTranscript
        {
            get => _isEditingTranscript;
            set
            {
                if (_isEditingTranscript != value)
                {
                    _isEditingTranscript = value;
                    OnPropertyChanged();
                }
            }
        }
        public string SummaryLanguage { get; set; } = string.Empty;
        public double SummaryConfidence { get; set; }
        public string BackendAudioId { get; set; } = string.Empty;
        public string SummaryStatus
        {
            get => _summaryStatus;
            set
            {
                if (_summaryStatus != value)
                {
                    _summaryStatus = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanSummarize));
                }
            }
        }
        private string _summaryStatus = "none";
        public string SummaryLanguageCode { get; set; } = string.Empty; // ISO dil kodu (tr, en, vs)

        public string TranscriptText { get; set; } = string.Empty;
        public string TranscriptLanguageCode { get; set; } = string.Empty;

        // UI Helper Properties
        [Ignore]
        public string FormattedDuration => Duration.ToString(@"mm\:ss");
        
        [Ignore]
        public string FormattedSize => FormatFileSize(FileSizeBytes);
        
        [Ignore]
        public string FormattedRecordedAt => RecordedAt.ToString("MMM dd, HH:mm");
        
        [Ignore]
        public bool CanPlay => !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);

        // Playback State
        private bool _isCurrentlyPlaying;
        
        [Ignore]
        public bool IsCurrentlyPlaying
        {
            get => _isCurrentlyPlaying;
            set
            {
                if (_isCurrentlyPlaying != value)
                {
                    _isCurrentlyPlaying = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PlayButtonText));
                }
            }
        }

        [Ignore]
        public string PlayButtonText => IsCurrentlyPlaying ? "⏸ Pause" : "▶ Play";

        public bool CanSummarize => !HasSummary && SummaryStatus != "queued" && SummaryStatus != "processing";

        private string FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            else if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            else
                return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // API DTOs ve Response Modelleri
    public class UploadResponse
    {
        public bool Success { get; set; }
        public string AudioId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Filename { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string UploadedAt { get; set; } = string.Empty;
    }

    public class TranscriptionResponse
    {
        public bool Success { get; set; }
        public string AudioId { get; set; } = string.Empty;
        public string JobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string EstimatedTime { get; set; } = string.Empty;
        
        // AssemblyAI API fields
        public string Text { get; set; } = string.Empty;           // Transcript text
        public string LanguageCode { get; set; } = string.Empty;   // Language code (e.g., "en_us", "tr")
        public double Confidence { get; set; }                     // Confidence score
        public int AudioDuration { get; set; }                     // Duration in seconds
    }

    public class SummaryResponse
    {
        public bool Success { get; set; }
        public string AudioId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public TranscriptionDto Transcription { get; set; } = new();
        public SummaryDto Summary { get; set; } = new();
        public string ProcessingTime { get; set; } = string.Empty;
    }

    public class TranscriptionDto
    {
        public string Text { get; set; } = string.Empty;
        public double Confidence { get; set; }
        public double Duration { get; set; }
        public string Language { get; set; } = string.Empty;  // Keep for backward compatibility
        public string LanguageCode { get; set; } = string.Empty;  // New field for language code
    }

    public class SummaryDto
    {
        public string Text { get; set; } = string.Empty;
        public List<string> KeyPoints { get; set; } = new();
        public string Sentiment { get; set; } = string.Empty;
        public List<string> Chapters { get; set; } = new();
    }

    public class StatusResponse
    {
        public bool Success { get; set; }
        public string AudioId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string EstimatedTimeRemaining { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
} 