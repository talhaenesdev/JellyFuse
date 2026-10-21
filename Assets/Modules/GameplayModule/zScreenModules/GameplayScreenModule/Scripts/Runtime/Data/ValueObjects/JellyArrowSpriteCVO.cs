using System;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects
{
    /// <summary>
    /// The arrow painted on an arrow-block jelly of one colour, for the way it moves and how many cells
    /// long the jelly is that way.
    /// </summary>
    [Serializable]
    public sealed class JellyArrowSpriteCVO
    {
        public JellyColour Colour;
        public JellyAxis Axis;
        [Tooltip("How many cells long the jelly is along Axis. A longer jelly takes the longest arrow there is.")]
        [Range(1, 4)] public int Length = 1;
        public Sprite Sprite;
    }
}
