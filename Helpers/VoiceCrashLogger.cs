using System;

namespace VoiceNotes.Helpers
{
    public static class VoiceCrashLogger
    {
        public static void Log(Exception ex, string? context = null)
        {
            // Burada breakpoint bırakabilirsin
            System.Diagnostics.Debug.WriteLine($"[VoiceCrashLogger] {context ?? "GENERIC"}: {ex.Message}\n{ex.StackTrace}");
            // TODO: Sentry veya başka bir servise gönder
        }

        public static void LogError(Exception ex, string? context = null)
        {
            // Error seviyesinde loglama
            System.Diagnostics.Debug.WriteLine($"[VoiceCrashLogger:ERROR] {context ?? "GENERIC"}: {ex.Message}\n{ex.StackTrace}");
            // TODO: Sentry veya başka bir servise gönder
        }
    }
} 