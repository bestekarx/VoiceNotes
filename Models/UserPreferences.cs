using SQLite;

namespace VoiceNotes.Models
{
    public class UserPreferences
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public string Theme { get; set; } = "System"; // Light, Dark, System
        public bool EnableNotifications { get; set; } = true;
        public bool ShareUsageData { get; set; } = false;
        public bool AutoSync { get; set; } = true;
    }
}