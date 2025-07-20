using VoiceNotes.ViewModels;

namespace VoiceNotes.Views
{
    public partial class WelcomePage : ContentPage
    {
        private WelcomeViewModel _viewModel;

        public WelcomePage(WelcomeViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.CurrentCarouselIndex = e.CurrentPosition;
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            
            // Hide navigation bar completely for immersive experience
            NavigationPage.SetHasNavigationBar(this, false);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            
            // Show navigation bar when leaving
            NavigationPage.SetHasNavigationBar(this, true);
        }
    }
} 