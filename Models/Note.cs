using SQLite;

namespace VoiceNotes.Models
{
    public class Note
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Title { get; set; } = string.Empty;
        
        // Legacy audio file path (will be deprecated)
        public string AudioFilePath { get; set; } = string.Empty;
        
        public DateTime Date { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // Navigation property (not stored in DB)
        [Ignore]
        public List<AudioRecord> AudioRecords { get; set; } = new();
        
        // Helper properties
        [Ignore]
        public bool HasAudioRecords => AudioRecords?.Any() == true;
        
        [Ignore]
        public int AudioRecordCount => AudioRecords?.Count ?? 0;
        
        [Ignore]
        public bool HasLegacyAudio => !string.IsNullOrEmpty(AudioFilePath);
    }
}
