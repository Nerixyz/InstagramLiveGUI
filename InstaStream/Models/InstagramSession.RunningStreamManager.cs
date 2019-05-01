using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;
using DynamicData;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramApiSharp.Helpers;

namespace InstaStream.Models
{
    public partial class InstagramSession
    {
        public ObservableCollection<StreamComment> Comments { get; set; }
        private DispatcherTimer _commentDispatcher;

        public ObservableCollection<StreamViewer> Viewers { get; set; }
        private DispatcherTimer _viewerDispatcher;

        private string lastTs = "";

        private const int MAX_CHAT_ITEMS = 300;
        private const int ITEMS_TO_REMOVE_COUNT = 25;

        public void StartStreamManager()
        {
            Comments = new ObservableCollection<StreamComment>();
            _commentDispatcher = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1.5)};
            _commentDispatcher.Tick += CommentDispatcherOnTick;
            _commentDispatcher.Start();
            API.LiveProcessor.EnableCommentsAsync(BroadcastId);

            Viewers = new ObservableCollection<StreamViewer>();
            _viewerDispatcher = new DispatcherTimer {Interval = TimeSpan.FromSeconds(1.5)};
            _viewerDispatcher.Tick += ViewerDispatcherOnTick;
            _viewerDispatcher.Start();
        }

        private async void ViewerDispatcherOnTick(object sender, EventArgs e)
        {
            try
            {
                IResult<InstaUserShortList> viewers = await API.LiveProcessor.GetViewerListAsync(BroadcastId);
                if (viewers.Succeeded)
                {
                    Viewers.RemoveMany(Viewers.Select(x =>
                    {
                        if (viewers.Value.Any(y => y.UserName.Equals(x.User)))
                            x.UpdateWatchTime();
                        else
                        {
                            Comments.Add(new StreamComment("system.viewers", x.User + " just left"));
                            return x;
                        }

                        return null;
                    }));
                    foreach (InstaUserShort s in viewers.Value)
                    {
                        if (Viewers.Any(x => x.User.Equals(s.UserName))) continue;
                        Viewers.Add(new StreamViewer(s.UserName, DateTime.Now));
                        Comments.Add(new StreamComment("system.viewers", s.UserName + " just joined"));
                    }
                }
            }
            catch (Exception ex)
            {
                Comments.Add(new StreamComment("system.viewers", ex.Message));
            }
        }

        private async void CommentDispatcherOnTick(object sender, EventArgs e)
        {
            try
            {
                //comments
                IResult<InstaBroadcastCommentList> comment =
                    await API.LiveProcessor.GetCommentsAsync(BroadcastId, lastTs, 10);
                if (comment.Succeeded && comment.Value.CommentCount > 0)
                {
                    long unixMax = 0;
                    comment.Value.Comments.ForEach(x =>
                    {
                        unixMax = Math.Max(unixMax, x.CreatedAt.ToUnixTime());
                            Comments.Add(new StreamComment(x.User.UserName, x.Text));
                        
                    });
                    lastTs = unixMax.ToString();

                    if (Comments.Count > MAX_CHAT_ITEMS)
                    {
                        Comments.RemoveMany(Enumerable.Range(0, ITEMS_TO_REMOVE_COUNT).Select(i => Comments[i]));
                    }
                }
                else if (!comment.Succeeded)
                {
                    Comments.Add(new StreamComment("system.comments.request", comment.Info.Message));
                }
            }
            catch (Exception ex)
            {
                Comments.Add(new StreamComment("system.comments", ex.Message));
            }
        }

        public void StopStreamManager()
        {
            _viewerDispatcher.Stop();
            _commentDispatcher.Stop();
        }

        public void SetTsToCreated(DateTime created)
        {
            lastTs = created.ToUnixTime().ToString();
        }
    }

    public class StreamViewer : Caliburn.Micro.PropertyChangedBase
    {
        private string _user;
        private string _watchTime;
        private readonly DateTime _startTime;

        public string User
        {
            get => _user;
            set
            {
                _user = value;
                NotifyOfPropertyChange();
            }
        }

        public string WatchTime
        {
            get => _watchTime;
            set
            {
                _watchTime = value;
                NotifyOfPropertyChange();
            }
        }

        public StreamViewer(string user, DateTime start)
        {
            _user = user;
            _startTime = start;
            UpdateWatchTime();
        }

        public void UpdateWatchTime()
        {
            TimeSpan ts = DateTime.Now - _startTime;
            StringBuilder builder = new StringBuilder("Watching ");

            if (ts.Days > 0)
                builder.Append(ts.Days + "d ");
            if (ts.Hours > 0)
                builder.Append(ts.Hours + "h ");
            if (ts.Minutes > 0)
                builder.Append(ts.Minutes + "m ");

            builder.Append(ts.Seconds + "s");
            WatchTime = builder.ToString();
        }
    }

    public class StreamComment : Caliburn.Micro.PropertyChangedBase
    {
        private string _user;
        private string _message;

        public string User
        {
            get => _user;
            set
            {
                _user = value;
                NotifyOfPropertyChange();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                NotifyOfPropertyChange();
            }
        }

        public StreamComment(string user, string message)
        {
            _user = user;
            _message = message;
        }
    }
}