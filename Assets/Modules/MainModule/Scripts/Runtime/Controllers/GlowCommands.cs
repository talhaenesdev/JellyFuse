using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.ConsoleModule;
using Modules.MainModule.Models;
using Modules.MainModule.Signals;
using UnityEngine;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// Lights the UI glow and starts watching the frame rate once the player is in. Every glow halo draws with
    /// the UI/Glow shader, which reads the global _UIGlowStrength: 1 lights them all, 0 hides them all at once.
    /// A device that switched the glow off before starts without it and is not watched.
    /// </summary>
    internal sealed class StartGlowWatchCommand : Command
    {
        public static readonly int STRENGTH = Shader.PropertyToID("_UIGlowStrength");

        [Inject] private GlowModel _model { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [InjectSignal] private MainInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            Shader.SetGlobalFloat(STRENGTH, _model.Off ? 0 : 1);
            if (_model.Off) return;
            _model.IsTicking = true;
            if (_model.IsHooked) return;
            _model.IsHooked = true;
            var model = _model;
            var provider = _updateProvider;
            var tick = _internalSignals.GlowTick;
            Action onFrame = null;
            onFrame = () =>
            {
                if (!model.IsTicking)
                {
                    provider.RemoveUpdate(onFrame);
                    model.IsHooked = false;
                    return;
                }
                tick.Dispatch();
            };
            provider.AddUpdate(onFrame);
        }
    }

    /// <summary>Counts this frame into the window.</summary>
    internal sealed class MeasureGlowFrameCommand : Command
    {
        [Inject] private GlowModel _model { get; set; }

        public override void Execute() => _model.AddFrame(Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Judges a full window against the target frame rate. The sequence goes on to switching the glow off only
    /// after SLOW_WINDOWS slow windows in a row; any other frame ends here.
    /// </summary>
    internal sealed class JudgeGlowCommand : Command
    {
        [Inject] private GlowModel _model { get; set; }
        [Inject] private FrameRateModel _frameRate { get; set; }

        public override void Execute()
        {
            Retain();
            if (!_model.WindowFull)
            {
                Stop();
                return;
            }
            float fps = _model.WindowFps;
            _model.StartWindow();
            int target = _frameRate.TargetFrameRate > 0 ? Mathf.Min(_frameRate.TargetFrameRate, 60) : 60;
            _model.SlowWindowsInRow = fps < target * GlowModel.SLOW_SHARE ? _model.SlowWindowsInRow + 1 : 0;
            if (_model.SlowWindowsInRow < GlowModel.SLOW_WINDOWS)
            {
                Stop();
                return;
            }
            FlowLogger.Log($"Glow - {fps:0} fps against a target of {target}: switching the UI glow off on this device.");
            Release();
        }
    }

    /// <summary>Switches the glow off for good on this device and stops the watch.</summary>
    internal sealed class SwitchGlowOffCommand : Command
    {
        [Inject] private GlowModel _model { get; set; }

        public override void Execute()
        {
            _model.SwitchOff();
            _model.IsTicking = false;
            Shader.SetGlobalFloat(StartGlowWatchCommand.STRENGTH, 0);
        }
    }
}
