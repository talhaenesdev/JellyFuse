using System;
using System.Collections.Generic;
using FlowIoC.BaseModule.Controller;
using FlowIoC.BaseModule.Injectable.Attributes;
using FlowIoC.BaseModule.Provider.Update;
using Modules.AudioModule.Enums;
using Modules.AudioModule.Models;
using Modules.AudioModule.Signals;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Modules.AudioModule.Controllers
{
    /// <summary>
    /// Puts the UI click listener on the frame: while it listens, every frame dispatches ClickTick.
    /// Hooks once, however many times it is asked.
    /// </summary>
    internal sealed class StartClickListeningCommand : Command
    {
        [Inject] private AudioModel _model { get; set; }
        [Inject] private IUpdateProvider _updateProvider { get; set; }
        [InjectSignal] private AudioInternalSignals _signals { get; set; }

        public override void Execute()
        {
            _model.IsListening = _model.Config != null && _model.Config.UiClickSounds;
            if (!_model.IsListening || _model.IsHooked) return;
            _model.IsHooked = true;
            var model = _model;
            var provider = _updateProvider;
            var tick = _signals.ClickTick;
            Action onFrame = null;
            onFrame = () =>
            {
                if (!model.IsListening)
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

    /// <summary>
    /// A press released this frame over an interactable button plays Tap. Read from the pointer and
    /// the EventSystem, so every button in every screen sounds without being wired to anything.
    /// </summary>
    internal sealed class DetectUiClickCommand : Command
    {
        [InjectSignal] private AudioInternalSignals _signals { get; set; }

        // A buffer reused by the pooled command, emptied on every run: no state carries over.
        private readonly List<RaycastResult> _hits = new();

        public override void Execute()
        {
            var pointer = UnityEngine.InputSystem.Pointer.current;
            var events = EventSystem.current;
            if (pointer == null || events == null || !pointer.press.wasReleasedThisFrame) return;
            var data = new PointerEventData(events) { position = pointer.position.ReadValue() };
            _hits.Clear();
            events.RaycastAll(data, _hits);
            if (_hits.Count == 0) return;
            var button = _hits[0].gameObject.GetComponentInParent<Selectable>();
            if (button != null && button.IsInteractable() && (button is Button || button is Toggle)) _signals.Play.Dispatch(AudioCue.Tap);
        }
    }
}
