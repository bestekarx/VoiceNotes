using System.Threading.Tasks;
using VoiceNotes.Models;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class PremiumService : IPremiumService
    {
        public async Task<bool> IsPremiumUser()
        {
            // For now, always return false (free tier)
            return await Task.FromResult(false);
        }

        public async Task<SubscriptionStatus> GetSubscriptionStatus()
        {
            // For now, always return Inactive
            return await Task.FromResult(SubscriptionStatus.Inactive);
        }

        public async Task<bool> PurchasePremium(SubscriptionType type)
        {
            VoiceCrashLogger.Log(new Exception($"Premium purchase attempted: {type}"));
            // Simulate purchase success
            return await Task.FromResult(true);
        }

        public async Task<bool> RestorePurchases()
        {
            VoiceCrashLogger.Log(new Exception("Restore purchases attempted"));
            // Simulate restore success
            return await Task.FromResult(true);
        }

        public async Task<UsageMetrics> GetUsageMetrics()
        {
            // Simulate usage metrics
            return await Task.FromResult(new UsageMetrics
            {
                TotalNotes = 5,
                AIProcessingHours = 0.5,
                TranslationRequests = 2,
                LastUsage = DateTime.Now
            });
        }

        public async Task<bool> HasEnoughCredits(double requiredHours)
        {
            // For now, always return true for free tier
            return await Task.FromResult(true);
        }

        public async Task DeductCredits(double hours)
        {
            VoiceCrashLogger.Log(new Exception($"Credits deducted: {hours}"));
            await Task.CompletedTask;
        }
    }
}