using System;
using System.Collections.Generic;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    [Serializable]
    public sealed class JellyColourCVO
    {
        public int Id = 3;
        public string Name = "Colour";
        public Color Tint = Color.green;
    }
    [Serializable]
    public sealed class JellyPaletteCVO
    {
        public JellyColour Colour;
        public string Name = "Colour";
        public Color Tint = Color.white;
        [Tooltip("Optional painted jelly for this colour, drawn as it is. Empty: the presentation's JellySprite tinted with Tint.")]
        public Sprite Sprite;
    }
    [Serializable]
    public sealed class JellyShapeCVO
    {
        public string Name = "Shape";
        public JellyColour Colour;
        public List<Vector2Int> Cells = new();
        [Tooltip("A jelly on the board only: frozen, it cannot be moved or fused until Thaw After jellies have gone through doors.")]
        public bool Frozen;
        [Tooltip("How many jellies must go through doors, anywhere on the board, before it thaws.")]
        [Min(1)] public int ThawAfter = 3;
        [Tooltip("A jelly on the board only: an arrow block moves only along Arrow Axis.")]
        public bool ArrowBlock;
        [Tooltip("The one way an arrow block can move.")]
        public JellyAxis ArrowAxis;
    }
    /// <summary>
    /// A door in the wall round the board: Size cells wide (1-4), one cell thick, on one side.
    /// Queue lists the jellies it takes, in order; only the first one still waiting can pass, and it
    /// must match it in colour and in shape. Queue shapes are local cells, (0, 0) at bottom-left.
    /// </summary>
    [Serializable]
    public sealed class JellyDoorCVO
    {
        public string Name = "Door";
        public JellySide Side = JellySide.Right;
        [Tooltip("The first cell along the side the door covers: a column on the top and bottom, a row on the left and right.")]
        public int Start;
        [Range(1, 4)] public int Size = 1;
        [Tooltip("On: for each jelly the door opens only as wide as that jelly, from its first cell, and the rest of it is wall. Off: it stays open across its whole size.")]
        public bool FitToJelly;
        [Tooltip("A jelly this door waits for, let go of in its rows (or columns) with up to this many free cells between it and the door, is drawn the rest of the way in. 0: it has to be put right in front of the door. -1: CD_JellyLevels' Default Pull Reach.")]
        [Range(-1, 10)] public int OwnPullReach = -1;
        [Tooltip("Frozen: the door takes nothing, and stands like wall, until Thaw After jellies have gone through the other doors.")]
        public bool Frozen;
        [Tooltip("How many jellies must go through the other doors before it thaws.")]
        [Min(1)] public int ThawAfter = 3;
        public List<JellyShapeCVO> Queue = new();
    }
    /// <summary>The exits levels were made with before doors. Only read to convert an old level (CD_JellyLevel.ConvertOldExits).</summary>
    [Serializable]
    public sealed class JellyTargetCVO
    {
        public string Name = "Target";
        public JellyColour Colour;
        [HideInInspector] public Vector2Int Direction = Vector2Int.right;
        [Tooltip("Gate order after the first target. Cells are local offsets from the first target bottom-left.")]
        public List<JellyShapeCVO> NextGates = new();
        public List<Vector2Int> Cells = new();
    }
}
