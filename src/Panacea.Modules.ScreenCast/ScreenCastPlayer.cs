using System;
using System.Threading.Tasks;
using Panacea.Core;
using Panacea.Modularity.AudioManager;
using Panacea.Modularity.Media;
using Panacea.Modularity.Media.Channels;
using Panacea.Modularity.MediaPlayerContainer;
using Panacea.Modularity.ScreenCast;
using Panacea.Modularity.TerminalPairing;
using Panacea.Modularity.UiManager;
using Panacea.Multilinguality;

namespace Panacea.Modules.ScreenCast
{
    public class MediaPlayerMessage
    {
        public string Action { get; set; }
        public string Mrl { get; set; }
        public string Extras { get; set; }
        public float Position { get; set; }
        public float Volume { get; set; }
        public double Duration { get; set; }
        public int HashCode { get; set; }
    }
    internal class ScreenCastPlayer : IScreenCastPlayer
    {
        private PanaceaServices _core;
        Translator translator = new Translator("ScreenCast");
        public IBoundTerminal BoundTerminal { get; }
        public IMediaResponse result { get; private set; }
        public ScreenCastPlayer(PanaceaServices core)
        {
            this._core = core;
            if (_core.TryGetPairing(out IBoundTerminalManager _pairing))
            {
                if (_pairing.IsBound())
                {
                    BoundTerminal = _pairing.GetBoundTerminal();
                    if (BoundTerminal.Relation == TerminalRelation.Slave)
                    {
                        BoundTerminal.On<MediaPlayerMessage>("mediaplayer", OnMessageFromMaster);
                    }
                    else
                    {
                        BoundTerminal.On<MediaPlayerMessage>("mediaplayer", OnMessageFromSlave);
                    }
                }
            }
        }


        /* Messages from master to slave to ask for action or information
         */
        private void OnMessageFromMaster(MediaPlayerMessage msg)
        {
            IMediaPlayerContainer container;
            IUiManager uiManager;
            IAudioManager _audio;
            switch (msg.Action)
            {
                case "subtitles-on":
                    try
                    {
                        result?.SetSubtitles(true);
                    }
                    catch
                    {
                        if (_core.TryGetUiManager(out uiManager))
                        {
                            uiManager.Toast(new Translator("ScreenCast").Translate("Subtitles not found for this media"));
                        }
                    }
                    break;
                case "subtitles-off":
                    try
                    {
                        result?.SetSubtitles(false);
                    }
                    catch
                    {
                        if (_core.TryGetUiManager(out uiManager))
                        {
                            uiManager.Toast(translator.Translate("Subtitles not found for this media"));
                        }
                    }
                    break;
                case "play":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        if (!string.IsNullOrEmpty(msg.Mrl))
                        {
                            result = container.Play(new MediaRequest(new IptvMedia() { URL = msg.Mrl + " " + msg.Extras }));
                            AttachToMediaResponse(result);
                        }
                        else
                        {
                            result.Play();
                        }
                    }
                    break;
                case "pause":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        result.Pause();
                    }
                    break;
                case "stop":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        result.Stop();
                    }
                    break;
                case "previous":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        result.Previous();
                    }
                    break;
                case "next":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        result.Next();
                    }
                    break;
                case "volume":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        if (_core.TryGetAudioManager(out _audio))
                        {
                            _audio.SpeakersVolume = (int)(msg.Volume * 100f);
                        }
                    }
                    break;
                case "getvolume":
                    if (_core.TryGetMediaPlayerContainer(out container))
                    {
                        if (_core.TryGetAudioManager(out _audio))
                        {
                            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage { Action = "getvolume", Volume = _audio.SpeakersVolume }));
                        }
                    }
                    break;
                default:
                    break;
            }
        }


        /* Messages sent from slave to master to inform about the current state
         */
        private void OnMessageFromSlave(MediaPlayerMessage msg)
        {
            switch (msg.Action)
            {
                case "playing":
                    Playing?.Invoke(this, null);
                    break;
                case "stopped":
                    Stopped?.Invoke(this, null);
                    break;
                case "error":
                    Error?.Invoke(this, null);
                    break;
                case "ended":
                    Ended?.Invoke(this, null);
                    break;
                case "now-playing":
                    NowPlaying?.Invoke(this, msg.Mrl);
                    break;
                case "position":
                    PositionChanged?.Invoke(this, msg.Position);
                    break;
                case "paused":
                    Paused?.Invoke(this, null);
                    break;
                case "opening":
                    Opening?.Invoke(this, null);
                    break;
                case "seekable-changed":
                    //TODO: Need to be able to send bool
                    break;
                case "pausable-changed":
                    //TODO: Need to be able to send bool
                    break;
                case "has-subtitles-changed":
                    //TODO: Need to be able to send bool
                    break;
                case "has-next-changed":
                    //TODO: Need to be able to send bool
                    break;
                case "duration-changed":
                    //TODO: Need to be able to send bool
                    break;
                default:
                    break;
            }
        }


        /* Slave attaches to media response's events and communicates every change to it's bound master.
         */
        void AttachToMediaResponse(IMediaResponse result)
        {
            result.Playing += Result_Playing;
            result.Ended += Result_Ended;
            result.Stopped += Result_Stopped;
            result.Error += Result_Error;
            result.NowPlaying += Result_NowPlaying;
            result.PositionChanged += Result_PositionChanged;
            result.Paused += Result_Paused;
            result.Opening += Result_Opening;
            result.IsSeekableChanged += Result_IsSeekableChanged;
            result.IsPausableChanged += Result_IsPausableChanged;
            result.HasSubtitlesChanged += Result_HasSubtitlesChanged;
            result.HasPreviousChanged += Result_HasPreviousChanged;
            result.HasNextChanged += Result_HasNextChanged;
            result.DurationChanged += Result_DurationChanged;
        }
        private void Result_DurationChanged(object sender, TimeSpan e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "duration-changed" }));
        }
        private void Result_HasNextChanged(object sender, bool e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "has-next-changed" }));
        }
        private void Result_HasPreviousChanged(object sender, bool e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "has-previous-changed" }));
        }
        private void Result_HasSubtitlesChanged(object sender, bool e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "has-subtitles-changed" }));
        }
        private void Result_IsPausableChanged(object sender, bool e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "pausable-changed" }));
        }
        private void Result_IsSeekableChanged(object sender, bool e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "seekable-changed" }));
        }
        private void Result_Opening(object sender, EventArgs e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "opening" }));
        }
        private void Result_Paused(object sender, EventArgs e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "paused" }));
        }
        private void Result_PositionChanged(object sender, float e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "position", Position = e }));
        }
        private void Result_NowPlaying(object sender, string e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "now-playing", Mrl = e }));
        }
        private void Result_Error(object sender, Exception e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "error" }));
        }
        private void Result_Stopped(object sender, EventArgs e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "stopped" }));
        }
        private void Result_Ended(object sender, EventArgs e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "ended" }));
        }
        private void Result_Playing(object sender, EventArgs e)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "playing" }));
        }


        /* Events invoked by master to to be used by other plugins in the master terminal
         */
        public event EventHandler<bool> IsSeekableChanged;
        public event EventHandler<float> PositionChanged;
        public event EventHandler<bool> HasNextChanged;
        public event EventHandler<bool> HasPreviousChanged;
        public event EventHandler<bool> IsPausableChanged;
        public event EventHandler<TimeSpan> DurationChanged;
        public event EventHandler<bool> HasSubtitlesChanged;
        public event EventHandler Opening;
        public event EventHandler Playing;
        public event EventHandler<string> NowPlaying;
        public event EventHandler Stopped;
        public event EventHandler Paused;
        public event EventHandler Ended;
        public event EventHandler<Exception> Error;


        /* Properties to keep inner state about the media now playing.
         */
        public bool IsSeekable { get => false; }
        private float _position;
        public float Position { get => _position; set => _position = value; }
        private bool _isPlaying;
        public bool IsPlaying { get => _isPlaying; }
        public bool HasNext { get => false; }
        public bool HasPrevious { get => false; }
        public bool IsPausable { get; }
        public TimeSpan Duration { get; }
        public bool HasSubtitles { get; }


        /* Public methods to be called on master from other plugins
         */
        public void Next()
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage { Action = "next" }));
        }
        public void Previous()
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage { Action = "previous" }));
        }
        public void Pause()
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "pause" }));
        }
        public void Play(string url, MediaItem media)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "play", Mrl = media.GetMRL(), Extras = media.GetExtras() }));
        }
        public void Play()
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "play" }));
        }
        public void SetSubtitles(bool on)
        {
            if (on)
            {
                Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "subtitles-on" }));
            }
            else
            {
                Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage() { Action = "subtitles-off" }));
            }
        }
        public void SetVolume(int value)
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage { Action = "volume", Volume = value }));
        }
        public void Stop()
        {
            Task.Run(() => BoundTerminal.Send("mediaplayer", new MediaPlayerMessage { Action = "stop" }));
        }

        /* Currently unused helper functions
         */
        private bool roleIs(TerminalRelation role)
        {
            return BoundTerminal.Relation == role;
        }
        private bool isBoundAs(TerminalRelation role, out IBoundTerminal boundTerminal)
        {
            boundTerminal = null;
            if (_core.TryGetPairing(out IBoundTerminalManager pairing))
            {
                if (pairing.IsBound() && pairing.GetBoundTerminal().Relation == role)
                {
                    var bound = pairing.GetBoundTerminal();
                    if (bound.Relation == role)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}