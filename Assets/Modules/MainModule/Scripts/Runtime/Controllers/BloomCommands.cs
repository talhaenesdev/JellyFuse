using System;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using FlowIoC.BaseModule.SharedData;
using FlowIoC.ConsoleModule;
using Modules.MainModule.Models;
using Modules.MainModule.Signals;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Modules.MainModule.Controllers
{
    /// <summary>
    /// Starts watching the frame rate once the player is in. A device that switched Bloom off before starts
    /// without it and is not watched; a scene with no UICamera filed has no Bloom to watch.
    /// </summary>
    internal sealed class StartBloomWatchCommand : Command
    {
        public const string CAMERA = "UICamera";

        [Inject] private BloomModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [InjectSignal] private MainInternalSignals _internalSignals { get; set; }

        public override void Execute()
        {
            var camera = _shared.GetMonoBehaviour<UniversalAdditionalCameraData>(CAMERA);
            if (camera == null) return;
            if (_model.Off)
            {
                camera.renderPostProcessing = false;
                return;
            }
            _model.IsTicking = true;
            if (_model.IsHooked) return;
            _model.IsHooked = true;
            var model = _model;
            var provider = _updateProvider;
            var tick = _internalSignals.BloomTick;
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
    internal sealed class MeasureBloomFrameCommand : Command
    {
        [Inject] private BloomModel _model { get; set; }

        public override void Execute() => _model.AddFrame(Time.unscaledDeltaTime);
    }

    /// <summary>
    /// Judges a full window against the target frame rate. The sequence goes on to switching Bloom off only after
    /// SLOW_WINDOWS slow windows in a row; any other frame ends here.
    /// </summary>
    internal sealed class JudgeBloomCommand : Command
    {
        [Inject] private BloomModel _model { get; set; }
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
            _model.SlowWindowsInRow = fps < target * BloomModel.SLOW_SHARE ? _model.SlowWindowsInRow + 1 : 0;
            if (_model.SlowWindowsInRow < BloomModel.SLOW_WINDOWS)
            {
                Stop();
                return;
            }
            FlowLogger.Log($"Bloom - {fps:0} fps against a target of {target}: switching Bloom off on this device.");
            Release();
        }
    }

    /// <summary>Switches Bloom off for good on this device and stops the watch.</summary>
    internal sealed class SwitchBloomOffCommand : Command
    {
        [Inject] private BloomModel _model { get; set; }
        [Inject] private ISharedDataModel _shared { get; set; }

        public override void Execute()
        {
            _model.SwitchOff();
            _model.IsTicking = false;
            var camera = _shared.GetMonoBehaviour<UniversalAdditionalCameraData>(StartBloomWatchCommand.CAMERA);
            if (camera != null) camera.renderPostProcessing = false;
        }
    }
}
