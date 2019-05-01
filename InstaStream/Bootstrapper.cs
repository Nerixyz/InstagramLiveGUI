using System.Windows;
using Caliburn.Micro;
using InstaStream.ViewModels;

namespace InstaStream
{
    public class Bootstrapper : BootstrapperBase
    {
        public Bootstrapper()
        {
            Initialize();
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);
            DisplayRootViewFor<MainWindowViewModel>();
        }
    }
}