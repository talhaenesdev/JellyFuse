using UnityEngine;

namespace Modules.GameplayModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// One profile character's frames for the board: the animal that walks the wall, is petted and eats from the
    /// bowl. The same slots and order as the board look's own cat, 16 frames to a row, so the board plays any of them
    /// the same way. It is addressable and remote: only the one the player picked is downloaded.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_BoardAnimal", menuName = "Jelly Fuse/Board Animal")]
    public sealed class CD_BoardAnimal : ScriptableObject
    {
        [Tooltip("Walk, sit idle, sit down, sleep, stretch, groom - 16 frames each.")]
        public Sprite[] Frames;
        [Tooltip("Petted, rub, reach, belly, hop, blink, curious, swat - 16 frames each.")]
        public Sprite[] TouchFrames;
        [Tooltip("Noticing food, eating, full - 16 frames each.")]
        public Sprite[] EatFrames;
        [Tooltip("16 frames of the quick move used when it is called over.")]
        public Sprite[] TrotFrames;
        [Tooltip("16 frames of the run to the bowl. A frame wider than the others draws the animal that much larger.")]
        public Sprite[] GallopFrames;
    }
}
