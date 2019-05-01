using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;

namespace InstaStream.Models
{
    public partial class InstagramSession : Caliburn.Micro.PropertyChangedBase
    {
        public IInstaApi API { get; private set; }
        public InstaSettings Settings { get; }
        public bool HasUserData => Settings.Username != null && Settings.Password != null;

        #region Streaming

        private bool _isStreaming;
        private bool _notify;
        private bool _setupObs = true;
        private bool _startObs = true;
        private string _broadcastID;
        private string _broadcastURL;
        private string _broadcastKey;
        private string _mediaId;
        private string _videoBitrate;
        private string _videoWidth;
        private string _videoRatio;
        private string _videoFps;

        public string VideoFps
        {
            get => _videoFps;
            set { _videoFps = value; NotifyOfPropertyChange();}
        }
        public string VideoWidth
        {
            get => _videoWidth;
            set { _videoWidth = value; NotifyOfPropertyChange();}
        }
        public string VideoRatio
        {
            get => _videoRatio;
            set { _videoRatio = value; NotifyOfPropertyChange();}
        }
        public string VideoBitrate
        {
            get => _videoBitrate;
            set { _videoBitrate = value; NotifyOfPropertyChange();}
        }
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                _isStreaming = value;
                NotifyOfPropertyChange();
            }
        }
        public string BroadcastId
        {
            get => _broadcastID;
            set
            {
                _broadcastID = value;
                NotifyOfPropertyChange();
            }
        }
        public string BroadcastKey
        {
            get => _broadcastKey;
            set { _broadcastKey = value; NotifyOfPropertyChange(); }
        }
        public string BroadcastUrl
        {
            get => _broadcastURL;
            set { _broadcastURL = value; NotifyOfPropertyChange(); }
        }
        public string MediaId
        {
            get => _mediaId;
            set { _mediaId = value; NotifyOfPropertyChange();}
        }

        public bool Notify
        {
            get => _notify;
            set { _notify = value; NotifyOfPropertyChange();}
        }

        public bool SetupObs
        {
            get => _setupObs;
            set { _setupObs = value; NotifyOfPropertyChange();}
        }

        public bool StartObs
        {
            get => _startObs;
            set { _startObs = value; NotifyOfPropertyChange();}
        }
        
        #endregion

        private bool _loggedIn;

        public bool LoggedIn
        {
            get => _loggedIn;
            set
            {
                _loggedIn = value;
                NotifyOfPropertyChange();
                if (!_loggedIn) return;
                SaveBin();
                SaveSettings();
            }
        }

        private string SettingsDir { get; set; }
        private string StateBinPath => Path.Combine(SettingsDir, "state.bin");
        private string SettingsFile => Path.Combine(SettingsDir, "settings.json");

        public InstagramSession()
        {
            SettingsDir = Path.Combine(Directory.GetCurrentDirectory(), "InstaLiveSettings");
            if (!Directory.Exists(SettingsDir))
                Directory.CreateDirectory(SettingsDir);

            Settings = File.Exists(SettingsFile) ? JsonConvert.DeserializeObject<InstaSettings>(File.ReadAllText(SettingsFile)) : new InstaSettings();
        }

        #region Streaming

        public void RegisterStream(InstaBroadcastCreate info, int initWidth)
        {
            string uploadUrl = info.UploadUrl.Replace(":443/", ":80/").Replace("rtmps://", "rtmp://");
            string[] parts = new Regex(info.BroadcastId.ToString()).Split(uploadUrl);
            BroadcastId = info.BroadcastId.ToString();
            BroadcastUrl = parts[0];
            BroadcastKey = BroadcastId + parts[1];
            VideoWidth = info.StreamVideoWidth.ToString();
            VideoRatio = (initWidth / (float) info.StreamVideoWidth).ToString();
            VideoBitrate = info.StreamVideoBitRate.ToString();
            VideoFps = info.StreamVideoFps.ToString();
        }

        public async Task<IResult<InstaBroadcastStart>> StartStream()
        {
            IResult<InstaBroadcastStart> broadcast = await API.LiveProcessor.StartAsync(BroadcastId, Notify);
            if (!broadcast.Succeeded) return broadcast;

            if (SetupObs)
            {
                OBSWebsocket sock = new OBSWebsocket();
                sock.Connect("ws://127.0.0.1:4444", null);
                sock.SetStreamingSettings(new StreamingService
                {
                    Type = "rtmp_custom",
                    Settings = new CustomRTMPStreamingService
                    {
                        AuthPassword = string.Empty,
                        AuthUsername = string.Empty,
                        ServerAddress = BroadcastUrl,
                        StreamKey = BroadcastKey,
                        UseAuthentication = false
                    }.ToJSON()
                }, true);

                if(StartObs)
                    sock.StartStreaming();
                
                sock.Disconnect();
            }


            MediaId = broadcast.Value.MediaId;
            IsStreaming = true;
            StartStreamManager();

            return broadcast;
        }

        public async Task<bool> StopStream()
        {
            IResult<bool> result = await API.LiveProcessor.EndAsync(BroadcastId);
            if(result.Succeeded)
                IsStreaming = !result.Value;

            if (!SetupObs) return result.Value;
            
            OBSWebsocket sock = new OBSWebsocket();
            sock.Connect("ws://127.0.0.1:4444", null);
            sock.StopStreaming();
            StopStreamManager();

            return result.Value;
        }

        public void CopyStreamUrl()
        {
            Clipboard.SetText(BroadcastUrl);
        }

        public void CopyStreamKey()
        {
            Clipboard.SetText(BroadcastKey);
        }

        #endregion

        #region Login

        public async Task<IResult<InstaLoginResult>> Login(UserSessionData data)
        {
            Settings.Username = data.UserName;
            Settings.Password = data.Password;
            API = InstaApiBuilder.CreateBuilder().SetUser(data).Build();

            if (File.Exists(StateBinPath) && data.UserName.Equals(Settings.Username))
            {
                using (FileStream fs = File.OpenRead(StateBinPath))
                    API.LoadStateDataFromStream(fs);
            }

            if (API.IsUserAuthenticated) return new Result<InstaLoginResult>(true, InstaLoginResult.Success);

            return await API.LoginAsync();
        }

        #endregion

        #region Settings

        public void SaveBin()
        {
            Stream state = API.GetStateDataAsStream();
            using (FileStream fileStream = File.Create(StateBinPath))
            {
                state.Seek(0, SeekOrigin.Begin);
                state.CopyTo(fileStream);
            }
        }

        public void SaveSettings()
        {
            File.WriteAllText(SettingsFile, JsonConvert.SerializeObject(Settings));
        }

        [Serializable]
        public class InstaSettings
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        #endregion
    }
}