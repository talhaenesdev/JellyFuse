using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using Modules.AudioModule.Models;
using Modules.AudioModule.Services;

namespace Modules.AudioModule.Controllers
{
    internal sealed class SetSoundsEnabledCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [SignalParam] private bool _enabled { get; set; }
        public override void Execute() => _model.SetSoundsEnabled(_enabled);
    }

    internal sealed class SetMusicEnabledCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [Inject] private IAudioPlayer _player { get; set; }
        [SignalParam] private bool _enabled { get; set; }

        public override void Execute()
        {
            _model.SetMusicEnabled(_enabled);
            PlayMusicCommand.ApplyMusic(_model, _player);
        }
    }
}
