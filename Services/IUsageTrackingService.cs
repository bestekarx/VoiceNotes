using System.Threading.Tasks;
using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IUsageTrackingService
    {
        Task TrackEvent(string eventName, Dictionary<string, string>? properties = null);
        Task TrackUsage(UsageMetrics metrics);
        Task<UsageMetrics> GetCurrentUsageMetrics();
    }
}