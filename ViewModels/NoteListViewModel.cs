using System.Collections.ObjectModel;
using VoiceNotes.Data;
using VoiceNotes.Models;
using VoiceNotes.Views;
using VoiceNotes.Services;
using VoiceNotes.Helpers;

namespace VoiceNotes.ViewModels
{
    public class NoteListViewModel : BaseViewModel
    {
        private readonly NoteDatabase _database;
        private readonly IAudioPlaybackService _audioPlaybackService;
        private bool _isRefreshing;

        public NoteListViewModel(
            NoteDatabase database, 
            IAudioPlaybackService audioPlaybackService)
        {
            _database = database;
            _audioPlaybackService = audioPlaybackService;
            
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

                System.Diagnostics.Debug.WriteLine("Loading notes from database");
                var notes = await _database.GetNotesAsync();
                
                Notes.Clear();
                foreach (var note in notes)
                {
                    Notes.Add(note);
                }

                System.Diagnostics.Debug.WriteLine($"Loaded {Notes.Count} notes");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error loading notes");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedToLoadNotes, AppResources.OK);
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
                System.Diagnostics.Debug.WriteLine("Navigating to create new note");
                await Shell.Current.GoToAsync(nameof(NoteDetailPage));
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "Error navigating to note detail");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedToCreateNote, AppResources.OK);
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
                System.Diagnostics.Debug.WriteLine($"Navigating to note detail for note {note.ID}");
                await Shell.Current.GoToAsync($"{nameof(NoteDetailPage)}?noteId={note.ID}");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, $"Error navigating to note detail for note {note.ID}");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.FailedToOpenNote, AppResources.OK);
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
                System.Diagnostics.Debug.WriteLine($"Deleting note {note.ID}");

                // First get all audio records for this note
                var audioRecords = await _database.GetAudioRecordsByNoteIdAsync(note.ID);
                System.Diagnostics.Debug.WriteLine($"Found {audioRecords.Count} audio records for note {note.ID}");

                // Delete the note (this should cascade delete audio records via database)
                await _database.DeleteNoteAsync(note);
                
                // Additionally check if old audio file path exists and delete it (backward compatibility)
                if (!string.IsNullOrEmpty(note.AudioFilePath) && File.Exists(note.AudioFilePath))
                {
                    try
                    {
                        File.Delete(note.AudioFilePath);
                        System.Diagnostics.Debug.WriteLine($"Deleted old audio file: {note.AudioFilePath}");
                    }
                    catch (Exception ex)
                    {
                        VoiceCrashLogger.LogError(ex, $"Failed to delete old audio file: {note.AudioFilePath}");
                    }
                }

                // Remove from UI collection
                Notes.Remove(note);

                System.Diagnostics.Debug.WriteLine($"Note {note.ID} deleted successfully");
                
                // Show success message without blocking
                _ = Shell.Current.DisplayAlert(AppResources.Success, AppResources.NoteDeletedSuccess, AppResources.OK);
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, $"Error deleting note {note?.ID}");
                await Shell.Current.DisplayAlert(AppResources.Error, AppResources.DeleteNoteError, AppResources.OK);
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
