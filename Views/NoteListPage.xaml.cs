using VoiceNotes.ViewModels;
using VoiceNotes.Models;

namespace VoiceNotes.Views;

public partial class NoteListPage : ContentPage
{
    private readonly NoteListViewModel _viewModel;

    public NoteListPage(NoteListViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadNotesAsync();
    }

    private async void OnNoteSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Note selectedNote)
        {
            // Clear selection
            ((CollectionView)sender).SelectedItem = null;

            // Navigate to detail page
            await _viewModel.GoToNoteDetailAsync(selectedNote);
        }
    }

    private async void OnDeleteButtonClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.BindingContext is Note note)
        {
            await _viewModel.DeleteNoteAsync(note);
        }
    }
}
