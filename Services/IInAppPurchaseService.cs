using System.Threading.Tasks;
using System.Collections.Generic;

namespace VoiceNotes.Services
{
    public interface IInAppPurchaseService
    {
        Task<bool> InitializeStore();
        Task<List<Product>> GetAvailableProducts();
        Task<bool> PurchaseProduct(string productId);
        Task<bool> RestorePurchases();
        Task<bool> ConsumeProduct(string productId);
    }

    public class Product
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }
}