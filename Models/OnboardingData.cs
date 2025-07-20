namespace VoiceNotes.Models
{
    public class OnboardingData
    {
        public int CurrentStep { get; set; }
        public string SelectedLanguage { get; set; } = "";
        public bool CanGoBack => CurrentStep > 0;
        public bool CanProceed { get; set; } = true;
    }
}