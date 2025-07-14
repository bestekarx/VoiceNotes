using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoiceNotes.Models;

namespace VoiceNotes.Data
{
    public class NoteDatabase
    {
        readonly SQLiteAsyncConnection _database;

        public NoteDatabase(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            _database.CreateTableAsync<Note>().Wait();
            _database.CreateTableAsync<AudioRecord>().Wait();
        }

        // Note operations
        public async Task<List<Note>> GetNotesAsync()
        {
            var notes = await _database.Table<Note>().OrderByDescending(n => n.Date).ToListAsync();
            
            // Load audio records for each note
            foreach (var note in notes)
            {
                note.AudioRecords = await GetAudioRecordsByNoteIdAsync(note.ID);
            }
            
            return notes;
        }

        public async Task<Note> GetNoteAsync(int id)
        {
            var note = await _database.Table<Note>().Where(i => i.ID == id).FirstOrDefaultAsync();
            if (note != null)
            {
                note.AudioRecords = await GetAudioRecordsByNoteIdAsync(note.ID);
            }
            return note;
        }

        public async Task<int> SaveNoteAsync(Note note)
        {
            note.UpdatedAt = DateTime.Now;
            
            if (note.ID != 0)
            {
                return await _database.UpdateAsync(note);
            }
            else
            {
                note.CreatedAt = DateTime.Now;
                return await _database.InsertAsync(note);
            }
        }

        public async Task<int> DeleteNoteAsync(Note note)
        {
            // Delete all associated audio records first
            var audioRecords = await GetAudioRecordsByNoteIdAsync(note.ID);
            foreach (var audioRecord in audioRecords)
            {
                await DeleteAudioRecordAsync(audioRecord);
            }
            
            return await _database.DeleteAsync(note);
        }

        // AudioRecord operations
        public async Task<List<AudioRecord>> GetAudioRecordsAsync()
        {
            try
            {
                return await _database.Table<AudioRecord>().ToListAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetAudioRecordsAsync Error: {ex.Message}");
                return new List<AudioRecord>();
            }
        }

        public async Task<List<AudioRecord>> GetAudioRecordsByNoteIdAsync(int noteId)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DATABASE] Getting audio records for note ID: {noteId}");
                
                var records = await _database.Table<AudioRecord>()
                    .Where(a => a.NoteID == noteId)
                    .OrderByDescending(a => a.RecordedAt)
                    .ToListAsync();
                
                System.Diagnostics.Debug.WriteLine($"[DATABASE] Found {records.Count} audio records for note {noteId}");
                
                foreach (var record in records)
                {
                    System.Diagnostics.Debug.WriteLine($"[DATABASE] - Record ID: {record.ID}, Title: {record.Title}, FilePath: {record.FilePath}");
                }
                
                return records;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DATABASE] Error getting audio records for note {noteId}: {ex.Message}");
                return new List<AudioRecord>();
            }
        }

        public Task<AudioRecord> GetAudioRecordAsync(int id)
        {
            return _database.Table<AudioRecord>().Where(a => a.ID == id).FirstOrDefaultAsync();
        }

        public async Task<int> SaveAudioRecordAsync(AudioRecord audioRecord)
        {
            if (audioRecord.ID != 0)
            {
                return await _database.UpdateAsync(audioRecord);
            }
            else
            {
                audioRecord.RecordedAt = DateTime.Now;
                return await _database.InsertAsync(audioRecord);
            }
        }

        public async Task<int> DeleteAudioRecordAsync(AudioRecord audioRecord)
        {
            // Delete physical file if exists
            if (!string.IsNullOrEmpty(audioRecord.FilePath) && File.Exists(audioRecord.FilePath))
            {
                try
                {
                    File.Delete(audioRecord.FilePath);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to delete audio file: {ex.Message}");
                }
            }
            
            return await _database.DeleteAsync(audioRecord);
        }
    }
}
