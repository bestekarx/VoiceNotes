using System.Collections.ObjectModel;
using VoiceNotes.Data;
using VoiceNotes.Models;
using VoiceNotes.Views;
using VoiceNotes.Services;
using VoiceNotes.Helpers;
using Microsoft.Extensions.Logging;

namespace VoiceNotes.ViewModels
{
    public class NoteListViewModel : BaseViewModel
    {
        private readonly NoteDatabase _database;
        private readonly IAudioPlaybackService _audioPlaybackService;
        private readonly ILogger<NoteListViewModel> _logger;
        private bool _isRefreshing;

        public NoteListViewModel(
            NoteDatabase database, 
            IAudioPlaybackService audioPlaybackService,
            ILogger<NoteListViewModel> logger)
        {
            _database = database;
            _audioPlaybackService = audioPlaybackService;
            _logger = logger;
            
            Notes = new ObservableCollection<Note>();
            LoadNotesCommand = new Command(async () => await LoadNotesAsync());
            AddNoteCommand = new Command(async () => await AddNoteAsync());
            DeleteNoteCommand = new Command<Note>(async (note) => await DeleteNoteAsync(note));
            GoToNoteDetailCommand = new Command<Note>(async (note) => await GoToNoteDetailAsync(note));
        }

        public ObservableCollection<Note> Notes { get; }

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set => SetProperty(ref _isRefreshing, value);
        }

        public Command LoadNotesCommand { get; }
        public Command AddNoteCommand { get; }
        public Command<Note> DeleteNoteCommand { get; }
        public Command<Note> GoToNoteDetailCommand { get; }

        public async Task LoadNotesAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                IsRefreshing = true;

                _logger.LogInformation("Loading notes from database");
                var notes = await _database.GetNotesAsync();
                
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }

                _logger.LogInformation("Loaded {NoteCount} notes", Notes.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading notes");
                VoiceCrashLogger.Log(ex, "LoadNotesAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to load notes. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
                IsRefreshing = false;
            }
        }

        public async Task AddNoteAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Navigating to create new note");
                await Shell.Current.GoToAsync(nameof(NoteDetailPage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to note detail");
                VoiceCrashLogger.Log(ex, "AddNoteAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to create new note. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task GoToNoteDetailAsync(Note note)
        {
            if (IsBusy || note == null)
                return;

            try
            {
                IsBusy = true;
                _logger.LogInformation("Navigating to note detail for note {NoteId}", note.ID);
                await Shell.Current.GoToAsync($"{nameof(NoteDetailPage)}?noteId={note.ID}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to note detail for note {NoteId}", note.ID);
                VoiceCrashLogger.Log(ex, "GoToNoteDetailAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to open note. Please try again.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteNoteAsync(Note note)
        {
            if (IsBusy || note == null)
                return;

            try
            {
                var confirm = await Shell.Current.DisplayAlert(
                    AppResources.DeleteNote, 
                    AppResources.DeleteNoteConfirm(note.Title), 
                    AppResources.Delete, 
                    AppResources.Cancel);

                if (!confirm)
                    return;

                IsBusy = true;
                _logger.LogInformation("Deleting note {NoteId}", note.ID);

                // First get all audio records for this note
                var audioRecords = await _database.GetAudioRecordsByNoteIdAsync(note.ID);
                _logger.LogInformation("Found {AudioRecordCount} audio records for note {NoteId}", audioRecords.Count, note.ID);

                // Delete the note (this should cascade delete audio records via database)
                await _database.DeleteNoteAsync(note);
                
                // Additionally check if old audio file path exists and delete it (backward compatibility)
                if (!string.IsNullOrEmpty(note.AudioFilePath) && File.Exists(note.AudioFilePath))
                {
                    try
                    {
                        File.Delete(note.AudioFilePath);
                        _logger.LogInformation("Deleted old audio file: {FilePath}", note.AudioFilePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete old audio file: {FilePath}", note.AudioFilePath);
                    }
                }

                // Remove from UI collection
                Notes.Remove(note);

                _logger.LogInformation("Note {NoteId} deleted successfully", note.ID);
                
                // Show success message without blocking
                _ = Shell.Current.DisplayAlert(AppResources.Success, AppResources.NoteDeletedSuccess, AppResources.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting note {NoteId}", note?.ID);
                VoiceCrashLogger.Log(ex, "DeleteNoteAsync");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.DeleteNoteError, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
