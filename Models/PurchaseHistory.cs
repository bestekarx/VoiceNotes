using SQLite;

namespace VoiceNotes.Models
{
    public class PurchaseHistory
    {
        [PrimaryKey, AutoIncrement]
        public int ID { get; set; }
        public int UserID { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
    }
}