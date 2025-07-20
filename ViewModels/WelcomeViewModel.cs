using VoiceNotes.Models;
using VoiceNotes.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Windows.Input;
using VoiceNotes.Helpers;
using VoiceNotes.Views;

namespace VoiceNotes.ViewModels
{
    public class WelcomeViewModel : BaseViewModel
    {
        private readonly IOnboardingService _onboardingService;
        private readonly ILogger<WelcomeViewModel> _logger;
        private OnboardingData _onboardingData;
        private int _currentCarouselIndex = 0;

        public WelcomeViewModel(
            IOnboardingService onboardingService,
            ILogger<WelcomeViewModel> logger)
        {
            _onboardingService = onboardingService;
            _logger = logger;
            _onboardingData = new OnboardingData();

            // Initialize commands
            NextCommand = new Command(async () => await NextStepAsync());
            PreviousCommand = new Command(async () => await PreviousStepAsync());
            SkipCommand = new Command(async () => await SkipOnboardingAsync());
            GetStartedCommand = new Command(async () => await GetStartedAsync());

            // Initialize onboarding slides
            InitializeOnboardingSlides();
        }

        public OnboardingData OnboardingData
        {
            get => _onboardingData;
            set
            {
                if (_onboardingData != value)
                {
                    _onboardingData = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurrentCarouselIndex
        {
            get => _currentCarouselIndex;
            set
            {
                if (_currentCarouselIndex != value)
                {
                    _currentCarouselIndex = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(CanProceed));
                    OnPropertyChanged(nameof(CanGoBack));
                    OnPropertyChanged(nameof(IsLastSlide));
                    OnPropertyChanged(nameof(ButtonText));
                }
            }
        }

        public bool CanProceed => true; // Her zaman aktif olsun
        public bool CanGoBack => CurrentCarouselIndex > 0;
        public bool IsLastSlide => CurrentCarouselIndex == OnboardingSlides.Count - 1;
        public string ButtonText => IsLastSlide ? "Başla" : "İleri";

        public ObservableCollection<OnboardingSlide> OnboardingSlides { get; private set; } = new();

        public ICommand NextCommand { get; }
        public ICommand PreviousCommand { get; }
        public ICommand SkipCommand { get; }
        public ICommand GetStartedCommand { get; }

        private void InitializeOnboardingSlides()
        {
            OnboardingSlides.Clear();
            OnboardingSlides.Add(new OnboardingSlide
            {
                Title = "Sesli Not Kaydetme",
                Subtitle = "Düşüncelerinizi anında kaydedin",
                Description = "Yüksek kaliteli ses kaydı ile akıllı özellikler",
                ImageSource = "voice_recording.png",
                IsFirstSlide = true
            });

            OnboardingSlides.Add(new OnboardingSlide
            {
                Title = "AI Destekli Özetler",
                Subtitle = "Sesli notlarınızın akıllı özetleri",
                Description = "Sesli notlarınızın zeki özetlerini alın",
                ImageSource = "ai_summary.png"
            });

            OnboardingSlides.Add(new OnboardingSlide
            {
                Title = "100+ Dil Desteği",
                Subtitle = "Herhangi bir dilde transkripsiyon",
                Description = "Herhangi bir dilde transkripsiyon ve çeviri",
                ImageSource = "translation.png"
            });

            OnboardingSlides.Add(new OnboardingSlide
            {
                Title = "Her Yerde Erişim",
                Subtitle = "Tüm cihazlarınızda senkronize",
                Description = "Tüm cihazlarınızda senkronize edin",
                ImageSource = "cloud_sync.png",
                IsLastSlide = true
            });
        }

        private async Task NextStepAsync()
        {
            try
            {
                if (IsLastSlide)
                {
                    // Son sayfadaysak Get Started'e git
                    await GetStartedAsync();
                }
                else if (CurrentCarouselIndex < OnboardingSlides.Count - 1)
                {
                    CurrentCarouselIndex++;
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "WelcomeViewModel.NextStepAsync");
                await ShowErrorAsync("Hata", "Bir sonraki adıma geçilemiyor.");
            }
        }

        private async Task PreviousStepAsync()
        {
            try
            {
                if (CurrentCarouselIndex > 0)
                {
                    CurrentCarouselIndex--;
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "WelcomeViewModel.PreviousStepAsync");
                await ShowErrorAsync("Hata", "Önceki adıma dönülemiyor.");
            }
        }

        private async Task SkipOnboardingAsync()
        {
            try
            {
                var result = await Shell.Current.DisplayAlert(
                    "Onboarding'i Atla",
                    "Kurulumu atlamak istediğinizden emin misiniz? Daha sonra ayarlardan tamamlayabilirsiniz.",
                    "Atla",
                    "İptal");

                if (result)
                {
                    // Onboarding'i tamamla
                    await _onboardingService.SetOnboardingCompleted();
                    
                    // Navigate to main app (NoteListPage)
                    await Shell.Current.GoToAsync("//NoteListPage");
                }
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "WelcomeViewModel.SkipOnboardingAsync");
                await ShowErrorAsync("Hata", "Onboarding atlanamıyor.");
            }
        }

        private async Task GetStartedAsync()
        {
            try
            {
                // Onboarding'i tamamla
                await _onboardingService.SetOnboardingCompleted();
                
                // Navigate to main app (NoteListPage)
                await Shell.Current.GoToAsync("//NoteListPage");
            }
            catch (Exception ex)
            {
                VoiceCrashLogger.LogError(ex, "WelcomeViewModel.GetStartedAsync");
                await ShowErrorAsync("Hata", "Onboarding başlatılamıyor.");
            }
        }

        private async Task ShowErrorAsync(string title, string message)
        {
            await Shell.Current.DisplayAlert(title, message, "Tamam");
        }
    }

    public class OnboardingSlide
    {
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImageSource { get; set; } = string.Empty;
        public bool IsFirstSlide { get; set; } = false;
        public bool IsLastSlide { get; set; } = false;
    }
} 