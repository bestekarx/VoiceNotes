using System.Globalization;
using System.Resources;

namespace VoiceNotes.Helpers
{
    public static class AppResources
    {
        private static readonly ResourceManager ResourceManager = 
            new ResourceManager("VoiceNotes.Resources.Strings.AppStrings", typeof(AppResources).Assembly);

        public static string GetString(string key)
        {
            return ResourceManager.GetString(key, CultureInfo.CurrentCulture) ?? key;
        }

        public static string GetString(string key, params object[] args)
        {
            var format = GetString(key);
            return string.Format(format, args);
        }

        // App General
        public static string AppTitle => GetString("AppTitle");
        public static string VoiceNotes => GetString("VoiceNotes");
        public static string TapToRecord => GetString("TapToRecord");

        // Buttons
        public static string Save => GetString("Save");
        public static string Delete => GetString("Delete");
        public static string Edit => GetString("Edit");
        public static string Cancel => GetString("Cancel");
        public static string OK => GetString("OK");
        public static string Refresh => GetString("Refresh");

        // Messages
        public static string Success => GetString("Success");
        public static string Error => GetString("Error");
        public static string Warning => GetString("Warning");

        // Note Operations
        public static string DeleteNote => GetString("DeleteNote");
        public static string DeleteNoteConfirm(string title) => GetString("DeleteNoteConfirm", title);
        public static string NoteDeletedSuccess => GetString("NoteDeletedSuccess");
        public static string NoteSavedSuccess => GetString("NoteSavedSuccess");
        public static string DeleteNoteError => GetString("DeleteNoteError");
        public static string SaveNoteError => GetString("SaveNoteError");
        public static string NoteNotFound => GetString("NoteNotFound");
        public static string EnterNoteTitle => GetString("EnterNoteTitle");
        public static string NoteTitlePlaceholder => GetString("NoteTitlePlaceholder");

        // Audio Operations
        public static string DeleteRecording => GetString("DeleteRecording");
        public static string DeleteRecordingConfirm(string title) => GetString("DeleteRecordingConfirm", title);
        public static string EditRecordingTitle => GetString("EditRecordingTitle");
        public static string EnterNewTitle => GetString("EnterNewTitle");
        public static string AudioFileNotFound => GetString("AudioFileNotFound");
        public static string PlayAudioError => GetString("PlayAudioError");
        public static string RecordingError => GetString("RecordingError");
        public static string MicrophonePermissionError => GetString("MicrophonePermissionError");
        public static string AudioNoteAvailable => GetString("AudioNoteAvailable");
        public static string ReadyToRecord => GetString("ReadyToRecord");
        public static string Recording => GetString("Recording");
        public static string RecordingSaved => GetString("RecordingSaved");
        public static string FailedToStartRecording => GetString("FailedToStartRecording");

        // AI Operations
        public static string AISummary => GetString("AISummary");
        public static string SummarizeAudio => GetString("SummarizeAudio");
        public static string SummarizationError(string error) => GetString("SummarizationError", error);
        public static string SummarizationFailed => GetString("SummarizationFailed");

        // Empty States
        public static string NoNotesYet => GetString("NoNotesYet");
        public static string CreateFirstNote => GetString("CreateFirstNote");
        public static string NoRecordingsYet => GetString("NoRecordingsYet");
        public static string StartRecording => GetString("StartRecording");

        // Labels
        public static string NoteTitle => GetString("NoteTitle");
        public static string AudioRecordings => GetString("AudioRecordings");
        public static string RecordingsCount(int count) => GetString("RecordingsCount", count);
    }
} 