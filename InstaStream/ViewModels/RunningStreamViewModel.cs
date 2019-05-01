using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;
using System.Windows.Threading;
using DynamicData;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Helpers;
using InstaStream.Models;
using Newtonsoft.Json.Linq;

namespace InstaStream.ViewModels
{
    public class RunningStreamViewModel : AbstractPage
    {
        public ObservableCollection<StreamComment> Comments { get; set; }
        
        public ObservableCollection<StreamViewer> Viewers { get; set; }

        private string _chatMessage;

        public string ChatMessage
        {
            get => _chatMessage;
            set { _chatMessage = value; NotifyOfPropertyChange();}
        }
        
        public RunningStreamViewModel(IHostScreen parent, InstagramSession session) : base(parent, session)
        {
            Comments = session.Comments;
            Viewers = session.Viewers;
            ChatMessage = string.Empty;
        }

        public async void SendChat()
        {
            string cmd = ChatMessage;
            ChatMessage = "";
            IResult<InstaComment> result = await session.API.LiveProcessor.CommentAsync(session.BroadcastId, cmd);
            if (result.Succeeded)
            {
                session.SetTsToCreated(result.Value.CreatedAt);
                Comments.Add(new StreamComment(session.API.GetLoggedUser().UserName, cmd));
            }
            else
            {
                Comments.Add(new StreamComment("system.comments", result.Info.Message));
            }
        }

       
    }
}