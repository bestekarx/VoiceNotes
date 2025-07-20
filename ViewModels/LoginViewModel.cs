using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using VoiceNotes.Helpers;

namespace VoiceNotes.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        public ICommand GoogleLoginCommand { get; }
        public ICommand AppleLoginCommand { get; }
        public ICommand SkipLoginCommand { get; }

        public LoginViewModel()
        {
            GoogleLoginCommand = new Command(async () => await GoogleLoginAsync());
            AppleLoginCommand = new Command(async () => await AppleLoginAsync());
            SkipLoginCommand = new Command(async () => await SkipLoginAsync());
        }

        private async Task GoogleLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Implement Google login logic here
                await Shell.Current.DisplayAlert("Login", "Google Login Clicked! (Not implemented)", "OK");
                // After successful login, navigate to main app
                await Shell.Current.GoToAsync("//NoteListPage");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "GoogleLoginAsync");
                await Shell.Current.DisplayAlert("Error", "Google login failed.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task AppleLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Implement Apple login logic here
                await Shell.Current.DisplayAlert("Login", "Apple Login Clicked! (Not implemented)", "OK");
                // After successful login, navigate to main app
                await Shell.Current.GoToAsync("//NoteListPage");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "AppleLoginAsync");
                await Shell.Current.DisplayAlert("Error", "Apple login failed.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task SkipLoginAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            try
            {
                // Navigate to main app
                await Shell.Current.GoToAsync("//NoteListPage");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "SkipLoginAsync");
                await Shell.Current.DisplayAlert("Error", "Failed to skip login.", "OK");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}