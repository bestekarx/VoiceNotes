using VoiceNotes.Views;

namespace VoiceNotes;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static void RegisterRoutes()
    {
        Routing.RegisterRoute(nameof(NoteListPage), typeof(NoteListPage));
        Routing.RegisterRoute(nameof(NoteDetailPage), typeof(NoteDetailPage));
    }
}
