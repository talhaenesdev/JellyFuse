using System;
using DG.Tweening;
using Modules.UiWidgetsModule.Enums;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.ValueObjects
{
    /// <summary>One effect of a text animation. The effects of one CD_TextAnimation play together.</summary>
    [Serializable]
    public sealed class TextAnimationCVO
    {
        public TextAnimationType Type = TextAnimationType.Pop;
        [Tooltip("Seconds it takes. For Pulse and Wave: one round.")]
        [Min(0)] public float Duration = .35f;
        [Tooltip("Seconds it waits before it starts.")]
        [Min(0)] public float Delay;
        public Ease Ease = Ease.OutBack;
        [Tooltip("How strong it is. Pop: the size it starts from (0.5 = half). Punch and Pulse: how much it swells (0.2 = a fifth). Shake and Wave: how far it moves, in canvas units.")]
        public float Strength = .5f;
        [Tooltip("Slide In: where it comes from, in canvas units from where it stands (0, -60 = from below).")]
        public Vector2 Offset = new(0, -60);
        [Tooltip("Typewriter and Wave: seconds between one letter and the next.")]
        [Min(0)] public float CharDelay = .03f;
        [Tooltip("Pulse: plays over and over, Duration a round. Wave always repeats.")]
        public bool Loop;
    }
}
