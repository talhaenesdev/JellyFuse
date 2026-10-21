using System;
using System.Collections.Generic;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects
{
    /// <summary>
    /// A picture of a jelly's shape for a door's screen. It is used for any jelly of that shape, turned
    /// or mirrored to match, and drawn in the jelly's colour. Cells count from the bottom-left of the
    /// shape as it is painted (x right, y up).
    /// </summary>
    [Serializable]
    public sealed class JellyShapeIconCVO
    {
        public Sprite Sprite;
        [Tooltip("The shape's cells as painted, from its bottom-left cell, x right and y up.")]
        public List<Vector2Int> Cells = new();
    }
}
