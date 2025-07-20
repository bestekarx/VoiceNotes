using SQLite;

namespace VoiceNotes.Models
{
    public class Subscription
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Type { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StoreTransactionId { get; set; } = string.Empty;
    }
}