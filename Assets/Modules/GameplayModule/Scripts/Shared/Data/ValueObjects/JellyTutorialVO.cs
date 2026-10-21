using System;
using System.Collections.Generic;
using UnityEngine;
namespace Modules.GameplayModule.Shared.Data.ValueObjects
{
    /// <summary>
    /// One move a level's tutorial makes the player do: the jelly on Path[0] is led through the cells in
    /// order, Path[0] travelling along them, and let go of at the last. A last cell in a door sends it out.
    /// A later step starts from the board as the earlier ones left it.
    /// </summary>
    [Serializable]
    public sealed class JellyTutorialStepCVO
    {
        [Tooltip("The cells the jelly's first cell goes through, in order: the first is on the jelly, the last is where it is let go of.")]
        public List<Vector2Int> Path = new();
        [Tooltip("The text shown while this step waits, read from the localization tables by this key. Empty: no text.")]
        public string TextKey;
        [Tooltip("The English shown when the key has no translation in the language in use.")]
        [TextArea] public string Text;

        /// <summary>Every cell from the first to the last, one grid step apart: across first, then up or down, between two clicks.</summary>
        public List<Vector2Int> Route()
        {
            var route = new List<Vector2Int>();
            if (Path.Count == 0) return route;
            var at = Path[0];
            route.Add(at);
            for (int i = 1; i < Path.Count; i++)
            {
                var to = Path[i];
                while (at.x != to.x) { at.x += Math.Sign(to.x - at.x); route.Add(at); }
                while (at.y != to.y) { at.y += Math.Sign(to.y - at.y); route.Add(at); }
            }
            return route;
        }
    }
    /// <summary>
    /// What a tutorial shows now: the cells left bright in the dark, the way the hand shows, and the cells the
    /// jelly is to end on, marked in its colour - empty when it ends in a door. All in board cells.
    /// </summary>
    public sealed class JellyTutorialRVO
    {
        public static readonly JellyTutorialRVO None = new(Array.Empty<Vector2Int>(), Array.Empty<Vector2>(), Array.Empty<Vector2Int>(), default, null, null);
        public IReadOnlyList<Vector2Int> Lit { get; }
        /// <summary>The middle of the jelly at each step of its way, in board cells.</summary>
        public IReadOnlyList<Vector2> HandPath { get; }
        public IReadOnlyList<Vector2Int> Target { get; }
        public Enums.JellyColour Colour { get; }
        /// <summary>The step's text: its localization key, and the English for when there is no translation.</summary>
        public string TextKey { get; }
        public string Text { get; }
        public bool Active => Lit.Count > 0;
        public JellyTutorialRVO(IReadOnlyList<Vector2Int> lit, IReadOnlyList<Vector2> handPath, IReadOnlyList<Vector2Int> target, Enums.JellyColour colour, string textKey, string text)
        { Lit = lit; HandPath = handPath; Target = target; Colour = colour; TextKey = textKey; Text = text; }
    }
}
