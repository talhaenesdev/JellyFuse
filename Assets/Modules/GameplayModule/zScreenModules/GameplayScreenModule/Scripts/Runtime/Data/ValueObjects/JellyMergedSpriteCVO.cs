using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects
{
    /// <summary>
    /// One painted picture of a whole jelly: a piece of this colour whose cells are exactly these is
    /// drawn with the one sprite instead of a block per cell. Cells count from the bottom-left of the
    /// shape (x right, y up), so the same shape anywhere on the board matches.
    /// </summary>
    [Serializable]
    public sealed class JellyMergedSpriteCVO
    {
        public JellyColour Colour;
        public Sprite Sprite;
        [Tooltip("The shape's cells from its bottom-left cell, x right and y up.")]
        public List<Vector2Int> Cells = new();
    }
}
