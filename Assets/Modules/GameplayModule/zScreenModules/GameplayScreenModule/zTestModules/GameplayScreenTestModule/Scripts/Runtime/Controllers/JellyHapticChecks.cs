#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Modules.GameplayModule.Controllers;
using Modules.GameplayModule.Models;
using Modules.GameplayModule.Shared.Data.ValueObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.HapticModule.Enums;
using Modules.HapticModule.Services;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.GameplayScreenTestModule.Controllers
{
    public sealed class JellyHapticChecks
    {
        private sealed class Probe : IHapticService
        {
            public bool Enabled { get; private set; } = true;
            public readonly List<HapticPreset> Effects = new();
            public void Play(HapticPreset preset) => Effects.Add(preset);
            public void PlayFor(HapticPreset preset, float seconds) => Effects.Add(preset);
            public void SetEnabled(bool enabled) => Enabled = enabled;
        }
        public string Run()
        {
            int checks = 0;
            void Check(bool ok, string name) { if (!ok) throw new Exception("Jelly haptic: " + name); checks++; }
            var model = new JellyPuzzleModel(); var feedback = new PuzzleFeedbackModel(); var probe = new Probe();
            var command = new PlayPuzzleFeedbackCommand();
            void Inject(string name, object value) => command.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(command, value);
            Inject("_model", model); Inject("_feedback", feedback); Inject("_haptics", probe);
            var pink = JellyColour.Pink;
            var pieces = new[] { new JellyPieceVO(1, pink, new[] { new Vector2Int(1, 1) }), new JellyPieceVO(2, pink, new[] { new Vector2Int(3, 1) }) };
            var domino = new JellyPieceVO(0, pink, new[] { Vector2Int.zero, Vector2Int.right });
            var gate = new JellyGateCVO(JellySide.Right, 1, 1, 7, 7, new[] { domino });
            model.Start(new JellyLevelCVO(7, 7, 60, Array.Empty<Vector2Int>(), pieces, new[] { gate }));
            void Drag(Vector2Int from, Vector2Int to, bool release)
            { feedback.Capture(model.Snapshot(), model.IsDragging); model.Drag(from, to, release); command.Execute(); }
            Drag(new(1, 1), new(1, 1), false);
            Check(probe.Effects.Count == 1 && probe.Effects[0] == HapticPreset.Selection, "One pickup effect");
            Drag(new(1, 1), new(1, 1), false);
            Check(probe.Effects.Count == 1, "Stationary pointer does not repeat");
            Drag(new(1, 1), new(2, 1), false);
            Check(probe.Effects.Count == 1, "Movement and touching do not vibrate");
            Drag(new(2, 1), new(2, 1), true);
            Check(probe.Effects.Count == 2 && probe.Effects[1] == HapticPreset.Medium, "Fuse on release effect");
            Drag(new(2, 1), new(7, 1), true);
            Check(model.Status == PuzzleStatus.Won && probe.Effects.Count == 3 && probe.Effects[2] == HapticPreset.Success, "Final exit gives only success");
            Drag(new(2, 1), new(7, 1), true);
            Check(probe.Effects.Count == 3, "Won board does not repeat success");
            model.Undo(); probe.Effects.Clear();
            feedback.Capture(model.Snapshot(), model.IsDragging); model.Tick(100); command.Execute();
            Check(probe.Effects.Count == 1 && probe.Effects[0] == HapticPreset.Failure, "Lost transition effect");
            feedback.Capture(model.Snapshot(), model.IsDragging); command.Execute();
            Check(probe.Effects.Count == 1, "Lost state does not repeat");
            var blue = new JellyPieceVO(3, JellyColour.Blue, new[] { new Vector2Int(1, 4) });
            model.Start(new JellyLevelCVO(7, 7, 60, Array.Empty<Vector2Int>(), new[] { new JellyPieceVO(1, pink, new[] { new Vector2Int(2, 1), new Vector2Int(3, 1) }), blue }, new[] { gate }));
            probe.Effects.Clear(); Drag(new(2, 1), new(7, 1), true);
            // A jelly going into its door trembles while it melts on screen; the feedback command adds nothing.
            Check(model.Status == PuzzleStatus.Playing && probe.Effects.Count == 0, "Nonfinal exit leaves the haptic to the melt");
            return "Jelly haptic: " + checks + " checks passed.";
        }
    }
}
#endif
