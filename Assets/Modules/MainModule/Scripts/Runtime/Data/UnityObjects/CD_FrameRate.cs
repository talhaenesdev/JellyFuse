using UnityEngine;

namespace Modules.MainModule.Data.UnityObjects
{
    /// <summary>The frame rate the game asks the device for. Filed on MainRoot.</summary>
    [CreateAssetMenu(fileName = "CD_FrameRate", menuName = "Main/Frame Rate")]
    public sealed class CD_FrameRate : ScriptableObject
    {
        [Tooltip("Frames per second the game runs at. Mobile runs at 30 when nothing is set; 60 keeps dragging smooth without the heat and battery cost of 90 or 120.")]
        [Range(15, 120)] public int TargetFrameRate = 60;
    }
}
