using Refit;
using VoiceNotes.Models;

namespace VoiceNotes.Services
{
    public interface IApiService
    {
        // Ses dosyası yükleme
        [Multipart]
        [Post("/api/audio/upload")]
        Task<UploadResponse> UploadAudioAsync([AliasAs("audio")] StreamPart file);

        // Transkripsiyon başlatma
        [Post("/api/audio/{id}/transcribe")]
        Task<TranscriptionResponse> TranscribeAudioAsync([AliasAs("id")] string audioId);

        // Özet alma
        [Get("/api/audio/{id}/summary")]
        Task<SummaryResponse> GetSummaryAsync([AliasAs("id")] string audioId);

        // Durum sorgulama
            [Get("/api/audio/{id}/status")]
        Task<StatusResponse> GetStatusAsync([AliasAs("id")] string audioId);
    }
} 