using DG.Tweening;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.UnityObjects
{
    /// <summary>
    /// How a button answers a press: it shrinks while held and springs back when let go. One asset
    /// shared by every ButtonPressAnimator, so all buttons feel the same and are tuned in one place.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_ButtonPress", menuName = "UI Widgets/Button Press")]
    public sealed class CD_ButtonPress : ScriptableObject
    {
        [Tooltip("The size the button shrinks to while held, as a share of its own (0.92 = eight percent smaller).")]
        [Range(.5f, 1.2f)] public float PressedScale = .92f;
        [Tooltip("Seconds it takes to shrink.")]
        [Min(0)] public float PressDuration = .07f;
        public Ease PressEase = Ease.OutQuad;
        [Tooltip("Seconds it takes to spring back.")]
        [Min(0)] public float ReleaseDuration = .22f;
        [Tooltip("OutBack overshoots a little past full size before it settles: the spring.")]
        public Ease ReleaseEase = Ease.OutBack;
        [Tooltip("Off: a button that cannot be pressed (not interactable) does not move either.")]
        public bool AnimateWhenDisabled;
    }
}
