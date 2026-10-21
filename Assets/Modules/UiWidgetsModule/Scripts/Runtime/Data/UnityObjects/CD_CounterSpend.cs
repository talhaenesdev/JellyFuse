using DG.Tweening;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.UnityObjects
{
    /// <summary>
    /// How a counter shows a spend (CounterSpendEffect): its number counts down, and copies of its icon drop away
    /// one after another - more of them the more is spent.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_CounterSpend", menuName = "UI Widgets/Counter Spend")]
    public sealed class CD_CounterSpend : ScriptableObject
    {
        [Header("Count")]
        [Tooltip("Seconds the number takes to count down to what is left.")]
        [Min(0)] public float CountSeconds = .6f;
        public Ease CountEase = Ease.OutCubic;
        [Tooltip("How much the counter's text swells while it counts, as a share of its size.")]
        [Range(0, .5f)] public float CountPunch = .12f;

        [Header("Dropping icons")]
        [Tooltip("One icon drops for every this many spent.")]
        [Min(1)] public int AmountPerIcon = 10;
        [Tooltip("The most icons that drop for one spend; at least one always does.")]
        [Range(1, 30)] public int MaxIcons = 8;
        [Tooltip("Seconds between one icon and the next.")]
        [Min(0)] public float Stagger = .07f;
        [Tooltip("Seconds an icon takes to drop away.")]
        [Min(.05f)] public float DropSeconds = .7f;
        [Tooltip("How far an icon drops, in canvas units.")]
        public float DropDistance = 180;
        [Tooltip("How far an icon may drift sideways, either way, in canvas units.")]
        public float DropSideways = 45;
        [Tooltip("How high an icon first hops before it falls, in canvas units.")]
        public float Hop = 30;
        [Tooltip("How far an icon turns as it drops, in degrees, either way.")]
        public float Spin = 160;
        [Tooltip("Its size at the start and the end, as a share of the icon's.")]
        public float StartScale = 1, EndScale = .55f;
    }
}
