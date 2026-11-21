using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// The electric combo's look: the frames the charged jellies crackle with, the burst they fuse in, the trail
    /// they fly to their door on and the ring the door answers with, and the badge that shows the combo.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_ElectricCombo", menuName = "Jelly Fuse/Electric Combo")]
    public sealed class CD_ElectricCombo : ScriptableObject
    {
        [Tooltip("Frames of the crackling outline round each charged jelly, played in a loop.")]
        public Sprite[] Aura = new Sprite[0];
        [Tooltip("Short arcs jumping between the charged jellies, swapped every frame step.")]
        public Sprite[] Arcs = new Sprite[0];
        [Tooltip("Small sparks popping round the charged jellies.")]
        public Sprite[] Sparks = new Sprite[0];
        [Tooltip("The burst the jellies fuse in, small to large.")]
        public Sprite[] Burst = new Sprite[0];
        [Tooltip("The ring the door answers with, small to large.")]
        public Sprite[] Ring = new Sprite[0];
        public Sprite GlowDot;
        public Sprite Trail;
        public Sprite Badge;

        [Header("Timing")]
        [Tooltip("Seconds each crackle frame stays.")]
        [Min(.02f)] public float FrameSeconds = .06f;
        [Tooltip("Share of the charge, at its end, in which the crackle draws in to one point.")]
        [Range(0, 1)] public float GatherShare = .3f;
        [Tooltip("Seconds the fused charge takes to fly to its door.")]
        [Min(.05f)] public float FlightSeconds = .35f;
        [Tooltip("How high the charged jellies rise, as a share of the grab lift.")]
        [Min(0)] public float LiftScale = 1.5f;

        [Header("Size (in cells)")]
        [Min(.1f)] public float AuraPadding = .45f;
        [Min(.1f)] public float BurstSize = 3.2f;
        [Min(.1f)] public float RingSize = 2.6f;
        [Min(.1f)] public float OrbSize = 1.3f;
        [Min(.1f)] public float SparkSize = .45f;
        [Min(1)] public int SparksAtOnce = 6;
    }
}
