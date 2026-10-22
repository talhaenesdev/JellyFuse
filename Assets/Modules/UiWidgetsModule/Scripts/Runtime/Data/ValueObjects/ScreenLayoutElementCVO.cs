using System;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.ValueObjects
{
    /// <summary>
    /// One object of a screen and how it is laid out. Only the ticked parts are applied; the rest is
    /// left as the prefab has it.
    /// </summary>
    [Serializable]
    public sealed class ScreenLayoutElementCVO
    {
        [Tooltip("The object's path under the screen's root, as in the Hierarchy: Pnl_Layout/Top_Panel.")]
        public string Path;

        [Tooltip("Applies Anchor Min, Anchor Max and Pivot.")]
        public bool OverrideAnchors;
        public Vector2 AnchorMin = new(.5f, .5f);
        public Vector2 AnchorMax = new(.5f, .5f);
        public Vector2 Pivot = new(.5f, .5f);

        [Tooltip("Applies Pos X / Pos Y, as the RectTransform shows them.")]
        public bool OverridePosition;
        public Vector2 Position;

        [Tooltip("Applies Width / Height (Size Delta, for a stretched side: how much it differs from its parent).")]
        public bool OverrideSize;
        public Vector2 Size = new(100, 100);

        [Tooltip("Applies the scale, the same on X and Y.")]
        public bool OverrideScale;
        [Min(0)] public float Scale = 1;

        [Tooltip("Applies whether the object is shown.")]
        public bool OverrideActive;
        public bool Active = true;
    }
}
