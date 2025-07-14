using VoiceNotes.ViewModels;
using VoiceNotes.Models;
using System.Globalization;
using Microsoft.Maui.Controls;

namespace VoiceNotes.Views;

public partial class NoteDetailPage : ContentPage, IQueryAttributable
{
    private readonly NoteDetailViewModel _viewModel;
    private int? _pendingNoteId;

    public NoteDetailPage(NoteDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("noteId", out var noteIdObj))
        {
            if (int.TryParse(noteIdObj.ToString(), out int noteId))
            {
                _pendingNoteId = noteId;
            }
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        if (_pendingNoteId.HasValue)
        {
            // Loading existing note
            await _viewModel.LoadNoteAsync(_pendingNoteId.Value);
            _pendingNoteId = null; // Reset after loading
        }
        else
        {
            // Creating new note
            await _viewModel.CreateNewNoteAsync();
        }
    }
}

public class ExpandCollapseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is bool b && b) ? "▲ Özeti Gizle" : "▼ Özeti Göster";
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
