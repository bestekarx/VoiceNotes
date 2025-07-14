using Refit;
using VoiceNotes.Models;
using VoiceNotes.Data;

namespace VoiceNotes.Services
{
    public class FileUploadService : IFileUploadService
    {
        private readonly IApiService _apiService;
        private readonly NoteDatabase _database;

        public FileUploadService(IApiService apiService, NoteDatabase database)
        {
            _apiService = apiService;
            _database = database;
        }

        public async Task<bool> UploadAudioRecordAsync(AudioRecord audioRecord)
        {
            try
            {
                if (!File.Exists(audioRecord.FilePath))
                    return false;

                using var fileStream = File.OpenRead(audioRecord.FilePath);
                var filePart = new StreamPart(fileStream, Path.GetFileName(audioRecord.FilePath), "audio/mpeg");
                var response = await _apiService.UploadAudioAsync(filePart);
                if (response != null && response.Success)
                {
                    audioRecord.BackendAudioId = response.AudioId;
                    audioRecord.IsUploaded = true;
                    audioRecord.FileName = response.Filename;
                    audioRecord.FileSizeBytes = response.FileSize;
                    await _database.SaveAudioRecordAsync(audioRecord);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                // TODO: Logging
                return false;
            }
        }

        public async Task<bool> UploadMultipleAudioRecordsAsync(List<AudioRecord> audioRecords)
        {
            var results = new List<bool>();
            
            foreach (var audioRecord in audioRecords)
            {
                var result = await UploadAudioRecordAsync(audioRecord);
                results.Add(result);
            }

            return results.All(r => r);
        }

        public async Task<List<AudioRecord>> GetUnuploadedAudioRecordsAsync()
        {
            try
            {
                var allAudioRecords = await _database.GetAudioRecordsAsync();
                return allAudioRecords.Where(a => !a.IsUploaded).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Get Unuploaded Error: {ex.Message}");
                return new List<AudioRecord>();
            }
        }

        public async Task<bool> SyncUnuploadedFilesAsync()
        {
            try
            {
                var unuploadedRecords = await GetUnuploadedAudioRecordsAsync();
                
                if (unuploadedRecords.Count == 0)
                {
                    return true;
                }

                return await UploadMultipleAudioRecordsAsync(unuploadedRecords);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Sync Error: {ex.Message}");
                return false;
            }
        }
    }
} 