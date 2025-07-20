
using SQLite;

namespace VoiceNotes.Models
{
    public class UserProfile
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string AgeGroup { get; set; } = "";
        public string UsagePurpose { get; set; } = "";
        public string LanguagePreference { get; set; } = "";
        public string RecordingFrequency { get; set; } = "";
        public string Expectations { get; set; } = "";
        public bool IsOnboardingCompleted { get; set; }
    }
}
