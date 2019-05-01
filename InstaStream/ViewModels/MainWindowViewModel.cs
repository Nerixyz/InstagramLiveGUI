using Caliburn.Micro;
using InstaStream.Models;

namespace InstaStream.ViewModels
{
    public class MainWindowViewModel : Conductor<AbstractPage>.Collection.OneActive, IHostScreen
    {
        public InstagramSession Session { get; }
        public MainWindowViewModel()
        {
            Session = new InstagramSession();
        }

        protected override void OnActivate()
        {
            base.OnActivate();
            ActivateItem(new LoginViewModel(this, Session));
        }

        public void ShowPage(AbstractPage page)
        {
            ActivateItem(page);
        }

        protected override void OnDeactivate(bool close)
        {
            if (Session.IsStreaming)
            {
                Session.StopStream().Wait();
            }
            base.OnDeactivate(close);
        }

        public async void StopStream()
        {
            IsLoading = true;
            if (await Session.StopStream())
            {
                IsLoading = false;
                ShowPage(new StartStreamViewModel(this, Session));
                return;
            }
            IsLoading = false;
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                NotifyOfPropertyChange();
            }
        }
    }
}