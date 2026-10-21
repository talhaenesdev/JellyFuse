using DG.Tweening;
using Modules.UiWidgetsModule.Shared.Enums;
using UnityEngine;

namespace Modules.UiWidgetsModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// How a panel with a PanelAnimator opens: the way it comes in, how long it takes, and the dim behind it.
    /// Published, so a screen can offer these numbers on the developer panel.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_PanelAnimation", menuName = "UI Widgets/Panel Animation")]
    public sealed class CD_PanelAnimation : ScriptableObject
    {
        public PanelOpenStyle Style = PanelOpenStyle.Pop;
        [Tooltip("Seconds the panel takes to come in. 0: it shows at once.")]
        [Range(0, 2)] public float Duration = .38f;
        public Ease Ease = Ease.OutBack;
        [Tooltip("Pop: the size it grows from, as a share of its size.")]
        [Range(0, 1)] public float StartScale = .7f;
        [Tooltip("Slide Up and Slide Down: how far it comes from, in canvas units.")]
        [Range(0, 1500)] public float SlideDistance = 400;
        [Tooltip("Seconds it takes to fade in, alongside the way it comes in. 0: no fade.")]
        [Range(0, 1)] public float FadeSeconds = .18f;
        [Tooltip("Seconds the dim behind the panel takes to fade in. 0: it shows at once.")]
        [Range(0, 1)] public float DimSeconds = .2f;
        [Tooltip("Seconds it waits before it starts.")]
        [Range(0, 1)] public float Delay;
        [Tooltip("Its buttons answer only once it is in, so a tap during the opening is not taken.")]
        public bool BlockInputWhileOpening = true;
    }
}
