using System.Threading.Tasks;
using System.Collections.Generic;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class InAppPurchaseService : IInAppPurchaseService
    {
        public async Task<bool> InitializeStore()
        {
            VoiceCrashLogger.Log(new Exception("Initializing in-app purchase store"));
            return await Task.FromResult(true);
        }

        public async Task<List<Product>> GetAvailableProducts()
        {
            VoiceCrashLogger.Log(new Exception("Getting available products"));
            return await Task.FromResult(new List<Product>
            {
                new Product { Id = "premium_monthly", Name = "Premium Monthly", Description = "Monthly subscription", Price = "$4.99" },
                new Product { Id = "premium_yearly", Name = "Premium Yearly", Description = "Yearly subscription", Price = "$49.99" }
            });
        }

        public async Task<bool> PurchaseProduct(string productId)
        {
            VoiceCrashLogger.Log(new Exception($"Purchasing product: {productId}"));
            return await Task.FromResult(true);
        }

        public async Task<bool> RestorePurchases()
        {
            VoiceCrashLogger.Log(new Exception("Restoring purchases"));
            return await Task.FromResult(true);
        }

        public async Task<bool> ConsumeProduct(string productId)
        {
            VoiceCrashLogger.Log(new Exception($"Consuming product: {productId}"));
            return await Task.FromResult(true);
        }
    }
}