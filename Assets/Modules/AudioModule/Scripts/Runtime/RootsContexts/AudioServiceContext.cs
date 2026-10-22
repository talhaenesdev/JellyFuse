using FlowIoC.BaseModule.Contexts;
using Modules.AudioModule.Controllers;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;
using Modules.AudioModule.Signals;

namespace Modules.AudioModule.RootsContexts
{
    public class AudioServiceContext : Context
    {
        private AudioInternalSignals _signals;
        private IAudioPlayer _player;

        public override void SignalBindings()
        {
            base.SignalBindings();
            _signals = InjectionBinder.Bind<AudioInternalSignals>();
        }

        public override void InjectionBindings()
        {
            base.InjectionBindings();
            InjectionBinder.Bind<AudioModel>();
            _player = InjectionBinder.Bind<IAudioPlayer, AudioPlayer>();
            InjectionBinderCrossContext.Bind<IAudioService, AudioService>();
        }

        public override void CommandBindings()
        {
            base.CommandBindings();
            CommandBinder.Bind(_signals.Play).ToSequence<PlayAudioCueCommand>();
            CommandBinder.Bind(_signals.PlayMusic).ToSequence<PlayMusicCommand>();
            CommandBinder.Bind(_signals.SetSoundsEnabled).ToSequence<SetSoundsEnabledCommand>();
            CommandBinder.Bind(_signals.SetMusicEnabled).ToSequence<SetMusicEnabledCommand>();
            CommandBinder.Bind(_signals.ListenForClicks).ToSequence<StartClickListeningCommand>();
            CommandBinder.Bind(_signals.ClickTick).ToSequence<DetectUiClickCommand>();
        }

        public override void Launch()
        {
            base.Launch();
            _signals.ListenForClicks.Dispatch();
        }

        public override void DestroyContext()
        {
            _player?.Dispose();
            base.DestroyContext();
        }
    }
}
