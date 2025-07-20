using VoiceNotes.Views;
using System.Windows.Input;

namespace VoiceNotes;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        
        // Register routes
        Routing.RegisterRoute(nameof(NoteDetailPage), typeof(NoteDetailPage));
        Routing.RegisterRoute(nameof(WelcomePage), typeof(WelcomePage));
        
        // Set binding context for flyout menu
        BindingContext = this;
    }

    public ICommand GoToNotesCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("//NoteListPage");
        Shell.Current.FlyoutIsPresented = false;
    });

    public ICommand GoToSettingsCommand => new Command(async () =>
    {
        await Shell.Current.GoToAsync("//SettingsPage");
        Shell.Current.FlyoutIsPresented = false;
    });
}
