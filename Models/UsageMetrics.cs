using SQLite;

namespace VoiceNotes.Models
{
    public class UsageMetrics
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int UserID { get; set; }
        public int TotalNotes { get; set; }
        public double AIProcessingHours { get; set; }
        public int TranslationRequests { get; set; }
        public DateTime LastUsage { get; set; }
        public bool IsWithinFreeTier => TotalNotes <= 3;
    }
}