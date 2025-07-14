using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using VoiceNotes.Data;
using VoiceNotes.Models;
using VoiceNotes.Services;
using VoiceNotes.Helpers;
using Microsoft.Extensions.Logging;

namespace VoiceNotes.ViewModels
{
    public class NoteDetailViewModel : BaseViewModel
    {
        private readonly NoteDatabase _database;
        private readonly IAudioRecordingService _audioRecordingService;
        private readonly IAudioPlaybackService _audioPlaybackService;
        private readonly IApiService _apiService;
        private readonly IFileUploadService _fileUploadService;
        private readonly ILogger<NoteDetailViewModel> _logger;
        
        private Note? _note;
        private string _title = string.Empty;
        private bool _isRecording;
        private bool _isPlaying;
        private string _recordingStatus = "Ready to record";
        private Color _recordingStatusColor = Colors.Gray;
        private TimeSpan _recordingDuration;
        
        public ObservableCollection<AudioRecord> AudioRecords { get; } = new();
        private readonly Queue<AudioRecord> _summaryQueue = new();
        private bool _isSummaryProcessing = false;
        private readonly object _queueLock = new();
        
        public NoteDetailViewModel(
            NoteDatabase database, 
            IAudioRecordingService audioRecordingService,
            IAudioPlaybackService audioPlaybackService,
            IApiService apiService,
            IFileUploadService fileUploadService,
            ILogger<NoteDetailViewModel> logger)
        {
            _database = database;
            _audioRecordingService = audioRecordingService;
            _audioPlaybackService = audioPlaybackService;
            _apiService = apiService;
            _fileUploadService = fileUploadService;
            _logger = logger;

            // Commands
            SaveNoteCommand = new Command(async () => await SaveNoteAsync(), () => CanSave);
            DeleteNoteCommand = new Command(async () => await DeleteNoteAsync(), () => CanDelete);
            RecordCommand = new Command(async () => await RecordAsync());
            PlayCommand = new Command(async () => await PlayAsync(), () => CanPlay);
            StopCommand = new Command(async () => await StopAsync(), () => CanStop);
            
            // Audio Record Commands
            PlayAudioRecordCommand = new Command<AudioRecord>(async (audio) => await PlayAudioRecordAsync(audio));
            DeleteAudioRecordCommand = new Command<AudioRecord>(async (audio) => await DeleteAudioRecordAsync(audio));
            EditAudioRecordCommand = new Command<AudioRecord>(async (audio) => await EditAudioRecordAsync(audio));
            SummarizeAudioCommand = new Command<AudioRecord>(async (audio) => await QueueSummaryAsync(audio), (audio) => CanSummarize(audio));
            ToggleSummaryCommand = new Command<AudioRecord>(ToggleSummary);

            // Re-AI Summary ve özet düzenleme için iki yeni komut ekliyorum: ReSummarizeAudioCommand ve EditSummaryCommand. Ayrıca özet düzenleme ve expand/collapse için gerekli metotları ekliyorum.
            ReSummarizeAudioCommand = new Command<AudioRecord>(async (audio) => await ReSummarizeAudioAsync(audio), (audio) => audio != null && audio.HasSummary);
            EditSummaryCommand = new Command<AudioRecord>(EditSummary);
            ToggleSummaryExpandCommand = new Command<AudioRecord>(ToggleSummaryExpand);

            // Transcript ve Summary düzenleme komutları
            EditTranscriptCommand = new Command<AudioRecord>(EditTranscript);
            EditSummaryCommand = new Command<AudioRecord>(EditSummary);

            // Subscribe to events
            _audioRecordingService.RecordingStatusChanged += OnRecordingStatusChanged;
            _audioRecordingService.RecordingDurationChanged += OnRecordingDurationChanged;
            _audioRecordingService.RecordingLimitReached += OnRecordingLimitReached;
            _audioPlaybackService.PlaybackStatusChanged += OnPlaybackStatusChanged;
            _audioPlaybackService.CurrentFileChanged += OnCurrentFileChanged;
        }

        public Note? Note
        {
            get => _note;
            set
            {
                if (SetProperty(ref _note, value))
                {
                    // Update Title when Note changes
                    Title = _note?.Title ?? string.Empty;
                    
                    // Update AudioRecords collection
                    AudioRecords.Clear();
                    if (_note?.AudioRecords != null)
                    {
                        foreach (var audioRecord in _note.AudioRecords)
                        {
                            AudioRecords.Add(audioRecord);
                        }
                    }
                    
                    OnPropertyChanged(nameof(HasAudioFile));
                    OnPropertyChanged(nameof(HasAudioRecords));
                    OnPropertyChanged(nameof(AudioFileName));
                    if (SaveNoteCommand is Command saveCmd) saveCmd.ChangeCanExecute();
                    if (DeleteNoteCommand is Command deleteCmd) deleteCmd.ChangeCanExecute();
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (SetProperty(ref _title, value))
                {
                    // Update Note.Title when Title changes
                    if (_note != null)
                    {
                        _note.Title = value;
                    }
                    if (SaveNoteCommand is Command saveCmd) saveCmd.ChangeCanExecute();
                }
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    OnPropertyChanged(nameof(RecordButtonText));
                    OnPropertyChanged(nameof(RecordButtonColor));
                    if (RecordCommand is Command recordCmd) recordCmd.ChangeCanExecute();
                    if (StopCommand is Command stopCmd) stopCmd.ChangeCanExecute();
                }
            }
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (SetProperty(ref _isPlaying, value))
                {
                    OnPropertyChanged(nameof(PlayButtonText));
                    if (PlayCommand is Command playCmd) playCmd.ChangeCanExecute();
                    if (StopCommand is Command stopCmd) stopCmd.ChangeCanExecute();
                }
            }
        }

        public string RecordingStatus
        {
            get => _recordingStatus;
            set => SetProperty(ref _recordingStatus, value);
        }

        public Color RecordingStatusColor
        {
            get => _recordingStatusColor;
            set => SetProperty(ref _recordingStatusColor, value);
        }

        public TimeSpan RecordingDuration
        {
            get => _recordingDuration;
            set => SetProperty(ref _recordingDuration, value);
        }

        public bool HasAudioFile => Note != null && !string.IsNullOrEmpty(Note.AudioFilePath);
        public bool HasAudioRecords => AudioRecords.Count > 0;
        public string AudioFileName => HasAudioFile ? Path.GetFileName(Note.AudioFilePath) : string.Empty;

        public string RecordButtonText => IsRecording ? "⏸" : "⏺";
        public Color RecordButtonColor => IsRecording ? Colors.Orange : Colors.Red;
        public string PlayButtonText => IsPlaying ? "⏸" : "▶";

        public bool CanSave => Note != null && !string.IsNullOrEmpty(Title);
        public bool CanDelete => Note != null && Note.ID > 0;
        public bool CanPlay => HasAudioFile && !IsRecording;
        public bool CanStop => IsRecording || IsPlaying;

        public ICommand SaveNoteCommand { get; }
        public ICommand DeleteNoteCommand { get; }
        public ICommand RecordCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopCommand { get; }
        
        // Audio Record Commands
        public ICommand PlayAudioRecordCommand { get; }
        public ICommand DeleteAudioRecordCommand { get; }
        public ICommand EditAudioRecordCommand { get; }
        public ICommand SummarizeAudioCommand { get; }
        public ICommand ToggleSummaryCommand { get; }
        public ICommand ReSummarizeAudioCommand { get; }
        public ICommand EditTranscriptCommand { get; }
        public ICommand EditSummaryCommand { get; }
        public ICommand ToggleSummaryExpandCommand { get; }

        public async Task CreateNewNoteAsync()
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("Creating new note");
                
                Note = new Note
                {
                    Title = "",
                    Date = DateTime.Now,
                    AudioFilePath = ""
                };
                
                Title = "";

                RecordingStatus = "Ready to record";
                RecordingStatusColor = Colors.Gray;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating new note");
                VoiceCrashLogger.Log(ex, "CreateNewNoteAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to create new note", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadNoteAsync(int noteId)
        {
            try
            {
                IsBusy = true;
                _logger.LogInformation("Loading note {NoteId}", noteId);
                System.Diagnostics.Debug.WriteLine($"[VIEWMODEL] Starting LoadNoteAsync for noteId: {noteId}");
                
                var note = await _database.GetNoteAsync(noteId);
                if (note == null)
                {
                    _logger.LogWarning("Note {NoteId} not found", noteId);
                    System.Diagnostics.Debug.WriteLine($"[VIEWMODEL] Note {noteId} not found in database");
                    await Shell.Current.DisplayAlert("Error", "Note not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                Note = note;
                await CheckAndResumeSummariesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading note {NoteId}", noteId);
                VoiceCrashLogger.Log(ex, "LoadNoteAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to load note", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task RecordAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                if (IsRecording)
                {
                    // Stop recording
                    _logger.LogInformation("Stopping recording");
                    var filePath = await _audioRecordingService.StopRecordingAsync();
                    
                    if (!string.IsNullOrEmpty(filePath) && Note != null)
                    {
                        // If Note is not saved yet, save it first with auto-generated title
                        if (Note.ID == 0)
                        {
                            _logger.LogInformation("Note not saved yet, auto-saving before creating audio record");
                            
                            // Auto-generate title if empty
                            if (string.IsNullOrWhiteSpace(Note.Title))
                            {
                                Note.Title = $"Untitled - {DateTime.Now:MMM dd, HH:mm}";
                                Title = Note.Title;
                            }
                            
                            await _database.SaveNoteAsync(Note);
                            _logger.LogInformation("Note auto-saved with ID: {NoteId} and title: {Title}", Note.ID, Note.Title);
                        }
                        
                        // Create new AudioRecord
                        var audioRecord = await _audioRecordingService.CreateAudioRecordAsync(Note.ID);
                        _logger.LogInformation("Created audio record with ID: {AudioId}", audioRecord.ID);
                        System.Diagnostics.Debug.WriteLine($"[RECORD] Created audio record: ID={audioRecord.ID}, NoteID={audioRecord.NoteID}, Title='{audioRecord.Title}', FilePath='{audioRecord.FilePath}'");
                        
                        // Save to database
                        await _database.SaveAudioRecordAsync(audioRecord);
                        _logger.LogInformation("Audio record saved to database");
                        System.Diagnostics.Debug.WriteLine($"[RECORD] Audio record saved to database with final ID: {audioRecord.ID}");
                        
                        // Add to collection
                        AudioRecords.Insert(0, audioRecord); // Insert at top for newest first
                        _logger.LogInformation("Audio record added to collection. Total count: {Count}", AudioRecords.Count);
                        System.Diagnostics.Debug.WriteLine($"[RECORD] Audio record added to collection. Total count: {AudioRecords.Count}");
                        System.Diagnostics.Debug.WriteLine($"[RECORD] HasAudioRecords after adding: {HasAudioRecords}");
                        
                        RecordingStatus = "Recording saved successfully";
                        RecordingStatusColor = Colors.Green;
                        
                        OnPropertyChanged(nameof(HasAudioRecords));
                        if (PlayCommand is Command playCmd) playCmd.ChangeCanExecute();
                    }
                }
                else
                {
                    // Start recording
                    _logger.LogInformation("Starting recording");
                    var success = await _audioRecordingService.StartRecordingAsync();
                    
                    if (success)
                    {
                        RecordingStatus = "Recording...";
                        RecordingStatusColor = Colors.Red;
                    }
                    else
                    {
                        RecordingStatus = "Failed to start recording";
                        RecordingStatusColor = Colors.Red;
                        await Shell.Current.DisplayAlert("Error", "Failed to start recording. Please check microphone permissions.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recording");
                VoiceCrashLogger.Log(ex, "RecordAsync");
                await Shell.Current.DisplayAlert("Error", "Recording failed. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task PlayAsync()
        {
            if (IsBusy || !HasAudioFile)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Playing audio file");

                if (IsPlaying)
                {
                    await _audioPlaybackService.StopAsync();
                }
                else
                {
                    var success = await _audioPlaybackService.PlayAsync(Note.AudioFilePath);
                    if (!success)
                    {
                        await Shell.Current.DisplayAlert("Error", "Failed to play audio", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing audio");
                VoiceCrashLogger.Log(ex, "PlayAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to play audio. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task StopAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Stopping audio operation");

                if (IsRecording)
                {
                    await RecordAsync(); // This will stop recording
                }
                else if (IsPlaying)
                {
                    await _audioPlaybackService.StopAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping audio operation");
                VoiceCrashLogger.Log(ex, "StopAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to stop audio operation", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SaveNoteAsync()
        {
            if (IsBusy || Note == null)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Saving note");

                if (string.IsNullOrEmpty(Title))
                {
                    await Shell.Current.DisplayAlert(AppResources.Error, AppResources.EnterNoteTitle, AppResources.OK);
                    return;
                }

                Note.Date = DateTime.Now;
                await _database.SaveNoteAsync(Note);
                
                _logger.LogInformation("Note saved successfully");
                await Shell.Current.DisplayAlert(AppResources.Success, AppResources.NoteSavedSuccess, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving note");
                VoiceCrashLogger.Log(ex, "SaveNoteAsync");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.SaveNoteError, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteNoteAsync()
        {
            if (IsBusy || Note == null)
                return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    "Delete Note", 
                    $"Are you sure you want to delete '{Title}'?", 
                    "Delete", 
                    "Cancel");

                if (!confirm)
                    return;

                IsBusy = true;
                _logger.LogInformation("Deleting note");

                await _database.DeleteNoteAsync(Note);
                
                // Delete audio file if exists
                if (HasAudioFile && File.Exists(Note.AudioFilePath))
                {
                    File.Delete(Note.AudioFilePath);
                }

                _logger.LogInformation("Note deleted successfully");
                await Shell.Current.DisplayAlert("Success", "Note deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note");
                VoiceCrashLogger.Log(ex, "DeleteNoteAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to delete note. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void OnRecordingStatusChanged(object? sender, bool isRecording)
        {
            IsRecording = isRecording;
        }

        private void OnRecordingDurationChanged(object? sender, TimeSpan duration)
        {
            RecordingDuration = duration;
        }

        private async void OnRecordingLimitReached(object? sender, string message)
        {
            await Shell.Current.DisplayAlert(AppResources.Warning, message, AppResources.OK);
        }

        private void OnPlaybackStatusChanged(object? sender, bool isPlaying)
        {
            IsPlaying = isPlaying;
        }

        private void OnCurrentFileChanged(object? sender, string? filePath)
        {
            // Update IsCurrentlyPlaying for all audio records
            foreach (var audioRecord in AudioRecords)
            {
                audioRecord.IsCurrentlyPlaying = !string.IsNullOrEmpty(filePath) && 
                                                audioRecord.FilePath == filePath;
            }
        }

        private void ToggleSummary(AudioRecord record)
        {
            if (record == null) return;
            record.IsSummaryExpanded = !record.IsSummaryExpanded;
            OnPropertyChanged(nameof(AudioRecords));
        }

        // Audio Record Management Methods
        public async Task PlayAudioRecordAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null || !audioRecord.CanPlay)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Playing audio record {AudioId}", audioRecord.ID);

                var success = await _audioPlaybackService.PlayAsync(audioRecord.FilePath);
                if (!success)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to play audio", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error playing audio record {AudioId}", audioRecord.ID);
                VoiceCrashLogger.Log(ex, "PlayAudioRecordAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to play audio. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteAudioRecordAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null)
                return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    AppResources.DeleteRecording, 
                    AppResources.DeleteRecordingConfirm(audioRecord.Title), 
                    AppResources.Delete, 
                    AppResources.Cancel);

                if (!confirm)
                    return;

                IsBusy = true;
                _logger.LogInformation("Deleting audio record {AudioId}", audioRecord.ID);

                // Delete from database
                await _database.DeleteAudioRecordAsync(audioRecord);
                
                // Delete audio file if exists
                if (!string.IsNullOrEmpty(audioRecord.FilePath) && File.Exists(audioRecord.FilePath))
                {
                    try
                    {
                        File.Delete(audioRecord.FilePath);
                        _logger.LogInformation("Audio file deleted: {FilePath}", audioRecord.FilePath);
                    }
                    catch (Exception fileEx)
                    {
                        _logger.LogWarning(fileEx, "Failed to delete audio file: {FilePath}", audioRecord.FilePath);
                    }
                }
                
                // Remove from collection
                AudioRecords.Remove(audioRecord);
                
                // Update UI
                OnPropertyChanged(nameof(HasAudioRecords));
                RecordingStatus = HasAudioRecords ? $"{AudioRecords.Count} recordings" : "Ready to record";
                RecordingStatusColor = HasAudioRecords ? Colors.Green : Colors.Gray;

                _logger.LogInformation("Audio record {AudioId} deleted successfully", audioRecord.ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio record {AudioId}", audioRecord.ID);
                VoiceCrashLogger.Log(ex, "DeleteAudioRecordAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to delete recording. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task EditAudioRecordAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null)
                return;

            try
            {
                var newTitle = await Shell.Current.DisplayPromptAsync(
                    AppResources.EditRecordingTitle, 
                    AppResources.EnterNewTitle, 
                    AppResources.Save, 
                    AppResources.Cancel, 
                    audioRecord.Title);

                if (string.IsNullOrWhiteSpace(newTitle))
                    return;

                IsBusy = true;
                _logger.LogInformation("Editing audio record {AudioId}", audioRecord.ID);

                audioRecord.Title = newTitle;
                await _database.SaveAudioRecordAsync(audioRecord);

                _logger.LogInformation("Audio record {AudioId} updated successfully", audioRecord.ID);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing audio record {AudioId}", audioRecord.ID);
                VoiceCrashLogger.Log(ex, "EditAudioRecordAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to update recording title. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task SummarizeAudioAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null)
                return;
            try
            {
                IsBusy = true;
                // 404 hatasını önlemek için: Dosya yüklü mü kontrol et
                if (!audioRecord.IsUploaded)
                {
                    var uploadSuccess = await _fileUploadService.UploadAudioRecordAsync(audioRecord);
                    if (!uploadSuccess)
                    {
                        await Shell.Current.DisplayAlert("Hata", "Ses kaydı sunucuya yüklenemedi.", "Tamam");
                        return;
                    }
                    audioRecord.IsUploaded = true;
                }
                // 1. Transkripsiyon başlat
                var backendAudioId = audioRecord.BackendAudioId;
                if (string.IsNullOrEmpty(backendAudioId))
                {
                    await Shell.Current.DisplayAlert("Hata", "Sunucu audioId bulunamadı. Lütfen kaydı tekrar yükleyin.", "Tamam");
                    return;
                }
                var transcribeResponse = await _apiService.TranscribeAudioAsync(backendAudioId);
                if (transcribeResponse == null || !transcribeResponse.Success)
                {
                    await Shell.Current.DisplayAlert("Hata", transcribeResponse?.Message ?? "Transkripsiyon başlatılamadı.", "Tamam");
                    return;
                }
                // 2. Özet alma (transkripsiyon tamamlanınca)
                var summaryResponse = await _apiService.GetSummaryAsync(backendAudioId);
                if (summaryResponse == null || !summaryResponse.Success)
                {
                    await Shell.Current.DisplayAlert("Hata", summaryResponse?.Summary?.Text ?? "Özet alınamadı.", "Tamam");
                    return;
                }
                // Sonucu modele işle ve UI'da göster
                audioRecord.HasSummary = true;
                audioRecord.SummaryText = summaryResponse.Summary.Text;
                //audioRecord.SummaryLanguage = summaryResponse.Transcription?.Language ?? "";
                audioRecord.SummaryConfidence = summaryResponse.Transcription?.Confidence ?? 0;
                OnPropertyChanged(nameof(AudioRecords));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", ex.Message, "Tamam");
                VoiceCrashLogger.Log(ex, "SummarizeAudioAsync");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task QueueSummaryAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null || audioRecord.HasSummary || audioRecord.SummaryStatus == "queued" || audioRecord.SummaryStatus == "processing")
                return;
            audioRecord.SummaryStatus = "queued";
            await _database.SaveAudioRecordAsync(audioRecord);
            _summaryQueue.Enqueue(audioRecord);
            ProcessSummaryQueue();
            OnPropertyChanged(nameof(AudioRecords));
        }

        private async void ProcessSummaryQueue()
        {
            lock (_queueLock)
            {
                if (_isSummaryProcessing) return;
                _isSummaryProcessing = true;
            }
            try
            {
                while (_summaryQueue.Count > 0)
                {
                    var audioRecord = _summaryQueue.Dequeue();
                    audioRecord.SummaryStatus = "processing";
                    await _database.SaveAudioRecordAsync(audioRecord);
                    OnPropertyChanged(nameof(AudioRecords));
                    try
                    {
                        // Upload kontrolü
                        if (!audioRecord.IsUploaded)
                        {
                            var uploadSuccess = await _fileUploadService.UploadAudioRecordAsync(audioRecord);
                            if (!uploadSuccess)
                            {
                                audioRecord.SummaryStatus = "failed";
                                await _database.SaveAudioRecordAsync(audioRecord);
                                OnPropertyChanged(nameof(AudioRecords));
                                await Shell.Current.DisplayAlert("Hata", $"Ses kaydı sunucuya yüklenemedi: {audioRecord.Title}", "Tamam");
                                continue;
                            }
                            audioRecord.IsUploaded = true;
                        }
                        var backendAudioId = audioRecord.BackendAudioId;
                        if (string.IsNullOrEmpty(backendAudioId))
                        {
                            audioRecord.SummaryStatus = "failed";
                            await _database.SaveAudioRecordAsync(audioRecord);
                            OnPropertyChanged(nameof(AudioRecords));
                            await Shell.Current.DisplayAlert("Hata", $"Sunucu audioId bulunamadı: {audioRecord.Title}", "Tamam");
                            continue;
                        }
                        var transcribeResponse = await _apiService.TranscribeAudioAsync(backendAudioId);
                        if (transcribeResponse == null || !transcribeResponse.Success)
                        {
                            audioRecord.SummaryStatus = "failed";
                            await _database.SaveAudioRecordAsync(audioRecord);
                            OnPropertyChanged(nameof(AudioRecords));
                            await Shell.Current.DisplayAlert("Hata", $"Transkripsiyon başlatılamadı: {audioRecord.Title}", "Tamam");
                            continue;
                        }
                        // Polling ile summary bekle
                        SummaryResponse summaryResponse = null;
                        for (int i = 0; i < 12; i++) // max 1 dakika bekle
                        {
                            try
                            {
                                summaryResponse = await _apiService.GetSummaryAsync(backendAudioId);
                                if (summaryResponse != null && summaryResponse.Success && summaryResponse.Status == "completed")
                                    break;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[SUMMARY POLL ERROR] {ex.Message}");
                                break;
                            }
                        }
                        if (summaryResponse == null || !summaryResponse.Success || summaryResponse.Status != "completed")
                        {
                            audioRecord.SummaryStatus = "failed";
                            await _database.SaveAudioRecordAsync(audioRecord);
                            OnPropertyChanged(nameof(AudioRecords));
                            await Shell.Current.DisplayAlert("Hata", $"AI özeti alınamadı: {audioRecord.Title}", "Tamam");
                            continue;
                        }
                        audioRecord.HasSummary = true;
                        audioRecord.SummaryText = summaryResponse.Summary.Text;
                        audioRecord.SummaryConfidence = summaryResponse.Transcription?.Confidence ?? 0;
                        audioRecord.SummaryStatus = "completed";
                        audioRecord.SummaryLanguageCode = summaryResponse.Transcription?.LanguageCode ?? "";
                        await _database.SaveAudioRecordAsync(audioRecord);
                        // Koleksiyonu refresh et
                        var idx = AudioRecords.IndexOf(audioRecord);
                        if (idx >= 0)
                        {
                            AudioRecords.RemoveAt(idx);
                            AudioRecords.Insert(idx, audioRecord);
                        }
                        OnPropertyChanged(nameof(AudioRecords));
                    }
                    catch (Exception ex)
                    {
                        audioRecord.SummaryStatus = "failed";
                        await _database.SaveAudioRecordAsync(audioRecord);
                        OnPropertyChanged(nameof(AudioRecords));
                        VoiceCrashLogger.Log(ex, "ProcessSummaryQueue");
                        await Shell.Current.DisplayAlert("Hata", $"AI özet kuyruğunda hata: {audioRecord.Title}\n{ex.Message}", "Tamam");
                    }
                }
            }
            finally
            {
                _isSummaryProcessing = false;
            }
        }

        public bool CanSummarize(AudioRecord audioRecord) => audioRecord != null && !audioRecord.HasSummary && audioRecord.SummaryStatus != "queued" && audioRecord.SummaryStatus != "processing";

        public async Task CheckAndResumeSummariesAsync()
        {
            foreach (var audio in AudioRecords)
            {
                if (!audio.HasSummary && (audio.SummaryStatus == "queued" || audio.SummaryStatus == "processing"))
                {
                    // Summary tamamlanmamışsa kuyruğa tekrar ekle
                    audio.SummaryStatus = "none";
                    await _database.SaveAudioRecordAsync(audio);
                    await QueueSummaryAsync(audio);
                }
            }
        }

        public async Task ReSummarizeAudioAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null) return;
            audioRecord.SummaryStatus = "none";
            audioRecord.HasSummary = false;
            audioRecord.SummaryText = string.Empty;
            await _database.SaveAudioRecordAsync(audioRecord);
            await QueueSummaryAsync(audioRecord);
        }
        public async Task GetTranscriptAndSummaryAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null) return;
            // 1. Transcript (speech-to-text)
            var transcriptResponse = await _apiService.TranscribeAudioAsync(audioRecord.BackendAudioId);
            if (transcriptResponse != null && transcriptResponse.Success)
            {
                audioRecord.TranscriptText = transcriptResponse.Text;
                audioRecord.TranscriptLanguageCode = transcriptResponse.LanguageCode;
            }
            // 2. Summary (AI özet)
            var summaryResponse = await _apiService.GetSummaryAsync(audioRecord.BackendAudioId);
            if (summaryResponse != null && summaryResponse.Success)
            {
                audioRecord.SummaryText = summaryResponse.Summary.Text;
                audioRecord.SummaryLanguageCode = summaryResponse.Transcription?.LanguageCode ?? "";
            }
            await _database.SaveAudioRecordAsync(audioRecord);
            var idx = AudioRecords.IndexOf(audioRecord);
            if (idx >= 0)
            {
                AudioRecords.RemoveAt(idx);
                AudioRecords.Insert(idx, audioRecord);
            }
            OnPropertyChanged(nameof(AudioRecords));
        }
        public void EditTranscript(AudioRecord audioRecord)
        {
            if (audioRecord == null) return;
            audioRecord.IsEditingTranscript = !audioRecord.IsEditingTranscript;
        }
        public void EditSummary(AudioRecord audioRecord)
        {
            if (audioRecord == null) return;
            audioRecord.IsEditingSummary = !audioRecord.IsEditingSummary;
        }
        public void ToggleSummaryExpand(AudioRecord audioRecord)
        {
            if (audioRecord == null) return;
            audioRecord.IsSummaryExpanded = !audioRecord.IsSummaryExpanded;
        }
    }
}
