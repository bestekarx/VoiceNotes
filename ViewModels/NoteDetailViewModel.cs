using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using VoiceNotes.Data;
using VoiceNotes.Models;
using VoiceNotes.Services;
using VoiceNotes.Helpers;

namespace VoiceNotes.ViewModels
{
    public class NoteDetailViewModel : BaseViewModel
    {
        private readonly NoteDatabase _database;
        private readonly IAudioRecordingService _audioRecordingService;
        private readonly IAudioPlaybackService _audioPlaybackService;
        private readonly IApiService _apiService;
        private readonly IFileUploadService _fileUploadService;
        
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
            IFileUploadService fileUploadService)
        {
            _database = database;
            _audioRecordingService = audioRecordingService;
            _audioPlaybackService = audioPlaybackService;
            _apiService = apiService;
            _fileUploadService = fileUploadService;

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

            ReSummarizeAudioCommand = new Command<AudioRecord>(async (audio) => await ReSummarizeAudioAsync(audio), (audio) => audio != null && audio.HasSummary);
            EditSummaryCommand = new Command<AudioRecord>(EditSummary);
            ToggleSummaryExpandCommand = new Command<AudioRecord>(ToggleSummaryExpand);

            EditTranscriptCommand = new Command<AudioRecord>(EditTranscript);

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
                    Title = _note?.Title ?? string.Empty;
                    
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

        public bool HasAudioFile => Note?.AudioFilePath != null && !string.IsNullOrEmpty(Note!.AudioFilePath);
        public bool HasAudioRecords => AudioRecords.Count > 0;
        public string AudioFileName => HasAudioFile ? Path.GetFileName(Note!.AudioFilePath) : string.Empty;

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
                System.Diagnostics.Debug.WriteLine("Creating new note");
                
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
                VoiceCrashLogger.LogError(ex, "Error creating new note");
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
                System.Diagnostics.Debug.WriteLine($"[VIEWMODEL] Starting LoadNoteAsync for noteId: {noteId}");
                
                var note = await _database.GetNoteAsync(noteId);
                if (note == null)
                {
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
                VoiceCrashLogger.LogError(ex, $"Error loading note {noteId}");
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
                    System.Diagnostics.Debug.WriteLine("Stopping recording");
                    var filePath = await _audioRecordingService.StopRecordingAsync();
                    
                    if (!string.IsNullOrEmpty(filePath) && Note != null)
                    {
                        if (Note.ID == 0)
                        {
                            System.Diagnostics.Debug.WriteLine("Note not saved yet, auto-saving before creating audio record");
                            
                            if (string.IsNullOrWhiteSpace(Note.Title))
                            {
                                Note.Title = $"Untitled - {DateTime.Now:MMM dd, HH:mm}";
                                Title = Note.Title;
                            }
                            
                            await _database.SaveNoteAsync(Note);
                            System.Diagnostics.Debug.WriteLine($"Note auto-saved with ID: {Note.ID} and title: {Note.Title}");
                        }
                        
                        var audioRecord = await _audioRecordingService.CreateAudioRecordAsync(Note.ID);
                        System.Diagnostics.Debug.WriteLine($"Created audio record with ID: {audioRecord.ID}");
                        
                        await _database.SaveAudioRecordAsync(audioRecord);
                        System.Diagnostics.Debug.WriteLine($"Audio record saved to database with final ID: {audioRecord.ID}");
                        
                        AudioRecords.Insert(0, audioRecord);
                        System.Diagnostics.Debug.WriteLine($"Audio record added to collection. Total count: {AudioRecords.Count}");
                        
                        RecordingStatus = "Recording saved successfully";
                        RecordingStatusColor = Colors.Green;
                        
                        OnPropertyChanged(nameof(HasAudioRecords));
                        if (PlayCommand is Command playCmd) playCmd.ChangeCanExecute();
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Starting recording");
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
                VoiceCrashLogger.LogError(ex, "Error during recording");
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
                System.Diagnostics.Debug.WriteLine("Playing audio file");

                if (IsPlaying)
                {
                    await _audioPlaybackService.StopAsync();
                }
                else
                {
                    if (Note?.AudioFilePath != null)
                    {
                        var success = await _audioPlaybackService.PlayAsync(Note.AudioFilePath);
                        if (!success)
                        {
                            await Shell.Current.DisplayAlert("Error", "Failed to play audio", "OK");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error playing audio");
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
                System.Diagnostics.Debug.WriteLine("Stopping audio operation");

                if (IsRecording)
                {
                    await RecordAsync();
                }
                else if (IsPlaying)
                {
                    await _audioPlaybackService.StopAsync();
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error stopping audio operation");
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
                System.Diagnostics.Debug.WriteLine("Saving note");

                if (string.IsNullOrEmpty(Title))
                {
                    await Shell.Current.DisplayAlert(AppResources.Error, AppResources.EnterNoteTitle, AppResources.OK);
                    return;
                }

                Note.Date = DateTime.Now;
                await _database.SaveNoteAsync(Note);
                
                System.Diagnostics.Debug.WriteLine("Note saved successfully");
                await Shell.Current.DisplayAlert(AppResources.Success, AppResources.NoteSavedSuccess, AppResources.OK);
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error saving note");
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
                System.Diagnostics.Debug.WriteLine("Deleting note");

                await _database.DeleteNoteAsync(Note);
                
                if (HasAudioFile && File.Exists(Note.AudioFilePath))
                {
                    File.Delete(Note.AudioFilePath);
                }

                System.Diagnostics.Debug.WriteLine("Note deleted successfully");
                await Shell.Current.DisplayAlert("Success", "Note deleted successfully", "OK");
                await Shell.Current.GoToAsync("..");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error deleting note");
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

        public async Task PlayAudioRecordAsync(AudioRecord audioRecord)
        {
            if (audioRecord == null || !audioRecord.CanPlay)
                return;

            try
            {
                IsBusy = true;
                System.Diagnostics.Debug.WriteLine($"Playing audio record {audioRecord.ID}");

                var success = await _audioPlaybackService.PlayAsync(audioRecord.FilePath);
                if (!success)
                {
                    await Shell.Current.DisplayAlert("Error", "Failed to play audio", "OK");
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, $"Error playing audio record {audioRecord.ID}");
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
                System.Diagnostics.Debug.WriteLine($"Deleting audio record {audioRecord.ID}");

                await _database.DeleteAudioRecordAsync(audioRecord);
                
                if (!string.IsNullOrEmpty(audioRecord.FilePath) && File.Exists(audioRecord.FilePath))
                {
                    try
                    {
                        File.Delete(audioRecord.FilePath);
                        System.Diagnostics.Debug.WriteLine($"Audio file deleted: {audioRecord.FilePath}");
                    }
                    catch (Exception fileEx)
                    {
                        VoiceCrashLogger.LogError(fileEx, $"Failed to delete audio file: {audioRecord.FilePath}");
                    }
                }
                
                AudioRecords.Remove(audioRecord);
                
                OnPropertyChanged(nameof(HasAudioRecords));
                RecordingStatus = HasAudioRecords ? $"{AudioRecords.Count} recordings" : "Ready to record";
                RecordingStatusColor = HasAudioRecords ? Colors.Green : Colors.Gray;

                System.Diagnostics.Debug.WriteLine($"Audio record {audioRecord.ID} deleted successfully");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, $"Error deleting audio record {audioRecord.ID}");
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
                System.Diagnostics.Debug.WriteLine($"Editing audio record {audioRecord.ID}");

                audioRecord.Title = newTitle;
                await _database.SaveAudioRecordAsync(audioRecord);

                System.Diagnostics.Debug.WriteLine($"Audio record {audioRecord.ID} updated successfully");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, $"Error editing audio record {audioRecord.ID}");
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
                var summaryResponse = await _apiService.GetSummaryAsync(backendAudioId);
                if (summaryResponse == null || !summaryResponse.Success)
                {
                    await Shell.Current.DisplayAlert("Hata", summaryResponse?.Summary?.Text ?? "Özet alınamadı.", "Tamam");
                    return;
                }
                audioRecord.HasSummary = true;
                audioRecord.SummaryText = summaryResponse.Summary.Text;
                audioRecord.SummaryConfidence = summaryResponse.Transcription?.Confidence ?? 0;
                OnPropertyChanged(nameof(AudioRecords));
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Hata", ex.Message, "Tamam");
                VoiceCrashLogger.LogError(ex, "SummarizeAudioAsync");
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
                        SummaryResponse? summaryResponse = null;
                        for (int i = 0; i < 12; i++)
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
                        VoiceCrashLogger.LogError(ex, "ProcessSummaryQueue");
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
            var transcriptResponse = await _apiService.TranscribeAudioAsync(audioRecord.BackendAudioId);
            if (transcriptResponse != null && transcriptResponse.Success)
            {
                audioRecord.TranscriptText = transcriptResponse.Text;
                audioRecord.TranscriptLanguageCode = transcriptResponse.LanguageCode;
            }
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