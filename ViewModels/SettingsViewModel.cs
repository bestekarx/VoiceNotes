using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VoiceNotes.Helpers;

namespace VoiceNotes.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private string _selectedLanguage = "Türkçe";

        public ObservableCollection<string> Languages { get; } = new()
        {
            "🇹🇷 Türkçe",
            "🇺🇸 English"
        };

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                if (SetProperty(ref _selectedLanguage, value))
                {
                    // Dil değişikliği işlemi burada yapılacak
                    OnLanguageChanged();
                }
            }
        }

        public SettingsViewModel()
        {
            // Title property'si BaseViewModel'de yok, kaldırıyoruz
        }

        private void OnLanguageChanged()
        {
            // Dil değişikliği işlemi
            // Burada CultureInfo değişikliği yapılabilir
        }
    }
} 