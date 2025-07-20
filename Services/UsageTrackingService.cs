using System.Threading.Tasks;
using VoiceNotes.Models;
using VoiceNotes.Helpers;

namespace VoiceNotes.Services
{
    public class UsageTrackingService : IUsageTrackingService
    {
        public async Task TrackEvent(string eventName, Dictionary<string, string>? properties = null)
        {
            System.Diagnostics.Debug.WriteLine($"[ANALYTICS] Event: {eventName}");
            if (properties != null)
            {
                foreach (var prop in properties)
                {
                    System.Diagnostics.Debug.WriteLine($"  {prop.Key}: {prop.Value}");
                }
            }
            await Task.CompletedTask;
        }

        public async Task TrackUsage(UsageMetrics metrics)
        {
            System.Diagnostics.Debug.WriteLine($"[USAGE] Total Notes: {metrics.TotalNotes}, AI Hours: {metrics.AIProcessingHours}");
            await Task.CompletedTask;
        }

        public async Task<UsageMetrics> GetCurrentUsageMetrics()
        {
            // Simulate fetching current usage metrics
            return await Task.FromResult(new UsageMetrics
            {
                TotalNotes = 0,
                AIProcessingHours = 0,
                TranslationRequests = 0,
                LastUsage = DateTime.MinValue
            });
        }
    }
}