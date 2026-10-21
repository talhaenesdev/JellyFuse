using Modules.LocalizationModule.Shared.Constants;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects
{
    /// <summary>
    /// How the jellies move and react: dragging, springs, the fuse hint, lifting, breathing and going
    /// into a door. Nothing here changes how the board looks at rest; that is CD_JellyBoardLook.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_JellyFeel", menuName = "Jelly Fuse/Feel")]
    public sealed class CD_JellyFeel : ScriptableObject
    {
        [Section("Dragging")]
        [Tooltip("Off: no wobble, stretch, pop or breathing, and their settings are hidden. Jellies still slide and lift smoothly.")]
        public bool AnimateJellies = true;
        [Tooltip("On: a held jelly moves cell by cell. Off: it glides with the finger between cells.")]
        public bool GridDrag = false;
        [Tooltip("How far a gliding jelly may lead its cell toward the finger, in cells. It never leads into a blocked cell.")]
        [Range(0, .5f)] public float DragFollow = .45f;
        [Tooltip("How fast a jelly slides along its route, in cells per second. It never overshoots or cuts a corner.")]
        [Range(2, 40)] public float SlideSpeed = 14;

        [Section("Lifting")]
        [Tooltip("How high a held jelly rises, in cells.")]
        [Range(0, .5f)] public float GrabLift = .16f;
        [Tooltip("Seconds a jelly takes to rise or settle when it is lifted or put down.")]
        [Range(0, .5f)] public float LiftTime = .08f;

        [Section("Springs")]
        [Tooltip("How fast the shape springs back. Higher wobbles quicker.")]
        [Range(50, 900)] public float JellyStiffness = 300;
        [Tooltip("Lower keeps wobbling longer.")]
        [Range(2, 60)] public float JellyDamping = 10;
        [Tooltip("Stretch per cell per second of speed.")]
        [Range(0, .3f)] public float MoveStretch = .08f;
        [Range(0, .6f)] public float MaxStretch = .3f;
        [Tooltip("How much a held jelly stretches toward the finger when pulled further than it can follow.")]
        [Range(0, 1)] public float PullStretch = .35f;
        [Tooltip("Spread when picked up, as a share of its size.")]
        [Range(0, .5f)] public float GrabPop = .15f;
        [Tooltip("Spread when put down.")]
        [Range(0, .5f)] public float LandSquash = .22f;
        [Tooltip("Spread when two jellies fuse.")]
        [Range(0, .5f)] public float FusePop = .25f;
        [Tooltip("How strongly jellies touching the moving one react. 0 = only the moving jelly wobbles.")]
        [Range(0, 1)] public float NeighbourWobble = 0;
        [Tooltip("Idle breathing while nothing touches it.")]
        [Range(0, .06f)] public float BreathAmount = .015f;
        [Range(0, 6)] public float BreathSpeed = 2.4f;

        [Section("Top and base")]
        [Tooltip("On: a jelly picture is drawn as its base (the darker band under it) and its top face, and the top moves on its own over a calmer base. Off: the picture moves as one.")]
        public bool SplitTopAndBase = true;
        [Tooltip("How much of the picture's height, from the bottom, is the base band. The placeholder jellies: 8 of 72 pixels.")]
        [Range(0, .5f)] public float BaseShare = .11f;
        [Tooltip("How far the base is drawn up under the top, as a share of the picture's height, so no gap opens between them while the top moves. Only the base band and this much show behind the top.")]
        [Range(0, .5f)] public float BaseOverlap = .08f;
        [Tooltip("How much of the jelly's stretch and squash the base takes; the top always takes all of it. 0 = the base stays still, 1 = it moves like the top.")]
        [Range(0, 1)] public float BaseMotion = .25f;
        [Tooltip("How far the top trails behind a moving jelly, per cell a second of speed.")]
        [Range(0, .2f)] public float TopLag = .04f;
        [Tooltip("The most the top slides off its base, in cells.")]
        [Range(0, .5f)] public float TopMaxOffset = .14f;
        [Tooltip("How much the top squashes down on its base when the jelly is picked up, put down or fused.")]
        [Range(0, .5f)] public float TopSquash = .2f;
        [Tooltip("The top's own spring: how quickly it comes back to rest.")]
        [Range(50, 900)] public float TopStiffness = 180f;
        [Tooltip("How quickly the top stops moving. Near twice the square root of Top Stiffness it settles without swinging back and forth.")]
        [Range(2, 60)] public float TopDamping = 26f;

        [Section("Rig")]
        [Tooltip("On: the top face bends and bulges as a soft mesh instead of sliding and scaling as one piece. Needs Split Top And Base.")]
        public bool UseRig = true;
        [Tooltip("Points across and up the mesh; more is smoother and costs a little more.")]
        [Range(1, 8)] public int RigColumns = 8;
        [Range(1, 8)] public int RigRows = 8;
        [Tooltip("How much of the top's trail bends it rather than sliding it: 1 bends it all, the bottom staying on the base.")]
        [Range(0, 1)] public float RigBend = 1;
        [Tooltip("How strongly a jelly moving up or down squashes and stretches, against how it leans moving sideways. 0: it only shifts.")]
        [Range(0, 4)] public float RigVerticalStretch = 1.5f;
        [Tooltip("On: the bottom edge moves the same way as the top - it bends, presses and bulges alike - and only the middle holds still; the base band follows the bottom edge. Off: the bottom holds on the base and only the top moves.")]
        public bool RigMirrorBottom = true;
        [Tooltip("How the bend grows from where the jelly holds still out to the moving edge: above 1 the still part is wider and the edge bends more.")]
        [Range(.5f, 4)] public float RigCurve = 1.5f;
        [Tooltip("How far the sides bulge out when the top is pressed down, as a share of its width.")]
        [Range(0, 2)] public float RigBulge = .45f;
        [Tooltip("How far the middle of the top edge dips when it is pressed down, as a share of its height.")]
        [Range(0, 1)] public float RigTopDip = .5f;

        [Section("Fuse hint")]
        [Tooltip("How much brighter a jelly glows while the held one would fuse with it on release.")]
        [Range(0, 1)] public float FuseHintGlow = .35f;
        [Tooltip("How high a jelly the held one would fuse with is lifted, in cells. Equal to GrabLift lifts both to the same height.")]
        [Range(0, .5f)] public float FuseReadyLift = .16f;
        [Tooltip("How much it swells with each pulse, as a share of its size.")]
        [Range(0, .15f)] public float FuseHintPulse = .05f;
        [Tooltip("Pulses per second times 2π.")]
        [Range(1, 20)] public float FuseHintSpeed = 8;

        [Section("Delivery")]
        [Tooltip("Seconds a delivered jelly takes to go into its door; what passes the door's edge is cut away. It plays even with AnimateJellies off.")]
        [Range(.15f, 1.5f)] public float DoorEnterTime = .55f;
        [Tooltip("Bits of jelly splashing from the door's mouth, per second for each cell of the jelly going in. 0 turns them off.")]
        [Range(0, 200)] public float DoorBits = 60;
        [Tooltip("Size of a bit, in cells (each one varies around it).")]
        [Range(.03f, .5f)] public float DoorBitSize = .16f;
        [Tooltip("How fast bits fly out, in cells per second.")]
        [Range(0, 12)] public float DoorBitSpeed = 4;
        [Tooltip("How far to the sides they spread: 0 straight back out of the door, 2 wide.")]
        [Range(0, 2)] public float DoorBitSpread = 1.2f;
        [Tooltip("Seconds a bit lives before it is gone.")]
        [Range(.1f, 2)] public float DoorBitLife = .5f;

        [Header("Fuse burst")]
        [Tooltip("Bits that burst from each cell of a jelly that has just fused. 0: no burst.")]
        [Range(0, 30)] public int FuseBitsPerCell = 6;
        [Tooltip("How fast fuse bits fly out, in cells per second.")]
        [Range(0, 10)] public float FuseBitSpeed = 2.5f;
        [Tooltip("Fuse bit size, in cells.")]
        [Range(.02f, .5f)] public float FuseBitSize = .14f;
        [Tooltip("Seconds a fuse bit lives.")]
        [Range(.1f, 2)] public float FuseBitLife = .45f;

        [Header("Booster burst")]
        [Tooltip("Bits that burst from each cell of a jelly a booster takes away (a rocket, a UFO). 0: no burst.")]
        [Range(0, 40)] public int VanishBitsPerCell = 14;
        [Tooltip("How fast those bits fly out, in cells per second.")]
        [Range(0, 15)] public float VanishBitSpeed = 4;
        [Tooltip("Their size, in cells.")]
        [Range(.02f, .5f)] public float VanishBitSize = .18f;
        [Tooltip("Seconds one lives.")]
        [Range(.1f, 2)] public float VanishBitLife = .6f;
        [Tooltip("On: a ring in the jelly's colour spreads from it too, as a fuse's does (Fuse Ring settings).")]
        public bool VanishRing = true;

        [Header("Fuse impact")]
        [Tooltip("How white a jelly flashes when it fuses, 0 to 1. 0: no flash.")]
        [Range(0, 1)] public float FuseFlash = .6f;
        [Tooltip("Seconds the flash takes to fade.")]
        [Range(0, 1)] public float FuseFlashTime = .25f;
        [Tooltip("How far the ring a fuse sends out spreads past the jelly, in cells. 0: no ring.")]
        [Range(0, 5)] public float FuseRingSize = 1.6f;
        [Tooltip("Seconds the ring takes to spread and fade.")]
        [Range(0, 2)] public float FuseRingTime = .45f;
        [Tooltip("How strong the ring is at its start.")]
        [Range(0, 1)] public float FuseRingAlpha = .8f;
        [Tooltip("How far the board shakes when jellies fuse, in cells. 0: no shake.")]
        [Range(0, .3f)] public float FuseShake = 0;
        [Tooltip("Seconds the shake lasts.")]
        [Range(0, 1)] public float FuseShakeTime = .22f;
    }
}
