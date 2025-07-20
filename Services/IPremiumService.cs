using System.Threading.Tasks;
using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IPremiumService
    {
        Task<bool> IsPremiumUser();
        Task<SubscriptionStatus> GetSubscriptionStatus();
        Task<bool> PurchasePremium(SubscriptionType type);
        Task<bool> RestorePurchases();
        Task<UsageMetrics> GetUsageMetrics();
        Task<bool> HasEnoughCredits(double requiredHours);
        Task DeductCredits(double hours);
    }

    public enum SubscriptionType
    {
        None,
        Monthly,
        Yearly
    }

    public enum SubscriptionStatus
    {
        Inactive,
        Active,
        Trial,
        GracePeriod,
        Expired
    }
}