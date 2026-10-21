using System;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects
{
    /// <summary>
    /// The screen painted on a door's picture and the grid on it, in the picture's pixels from its bottom-left:
    /// the jelly the door waits for is drawn on it a grid square a cell.
    /// </summary>
    [Serializable]
    public sealed class JellyDoorScreenCVO
    {
        [Tooltip("The screen's area on the door's picture. Zero width: the door has no screen, and its jelly is drawn the old way.")]
        public Rect Screen;
        [Tooltip("Where one vertical and one horizontal grid line cross.")]
        public Vector2 GridOrigin;
        [Tooltip("Pixels from one grid line to the next.")]
        [Min(1)] public float Pitch = 20;
    }
}
