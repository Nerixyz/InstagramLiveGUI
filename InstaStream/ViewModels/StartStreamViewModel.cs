using System.IO;
using System.Threading.Tasks;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstaStream.Models;
using MaterialDesignThemes.Wpf;

namespace InstaStream.ViewModels
{
    public class StartStreamViewModel : AbstractPage
    {
        private string _width;
        private string _height;
        private string _message;

        public string Width
        {
            get => _width;
            set { _width = value; NotifyOfPropertyChange();}
        }

        public string Height
        {
            get => _height;
            set { _height = value; NotifyOfPropertyChange();}
        }

        public string Message
        {
            get => _message;
            set { _message = value; NotifyOfPropertyChange();}
        }

        private const string START_DIALOG = "StreamInfo";
        
        
        public StartStreamViewModel(IHostScreen parent, InstagramSession session) : base(parent, session)
        {
            Message = "";
            Height = "1920";
            Width = "1080";
            
        }

        public async void StartStream()
        {
            parent.IsLoading = true;
            if(!int.TryParse(Width, out int width) || !int.TryParse(Height, out int height))
                return;

            IResult<InstaBroadcastCreate> create = await session.API.LiveProcessor.CreateAsync(width, height, Message);
            parent.IsLoading = false;
            if(!create.Succeeded)
                return;
            
            session.RegisterStream(create.Value, width);
            await DialogHost.Show(session, START_DIALOG);
            parent.IsLoading = true;
            IResult<InstaBroadcastStart> result = await session.StartStream();
            parent.IsLoading = false;
            if(!result.Succeeded)
                return;
            
            parent.ShowPage(new RunningStreamViewModel(parent, session));
        }
    }
}