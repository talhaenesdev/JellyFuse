using System;
using System.Collections.Generic;
using Modules.UiWidgetsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.UnityObjects
{
    /// <summary>
    /// How a screen is laid out on the device: the size its content is designed at, the margins it
    /// keeps, and the objects whose place and size this asset decides rather than the prefab. A
    /// ScreenLayout on the screen's root applies it; edited during Play, the screen follows at once.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_ScreenLayout", menuName = "UI Widgets/Screen Layout")]
    public sealed class CD_ScreenLayout : ScriptableObject
    {
        [Header("Fit")]
        [Tooltip("The width the content is designed at, in canvas units. The screen scales it to fill the device's width.")]
        [Min(1)] public float DesignWidth = 760;
        [Tooltip("The least height the content needs, in canvas units. A short, wide screen fits this height instead of the width.")]
        [Min(1)] public float MinHeight = 1000;
        [Tooltip("Space kept at the left and right of the content, in canvas units.")]
        [Min(0)] public float HorizontalMargin;
        [Tooltip("Space kept above and below the content, in canvas units.")]
        [Min(0)] public float VerticalMargin;
        [Tooltip("The largest the content is scaled. 0 for no limit.")]
        [Min(0)] public float MaxScale;

        [Header("Objects")]
        public List<ScreenLayoutElementCVO> Elements = new();

        /// <summary>Raised when the asset is edited in the Inspector, so an open screen lays itself out again.</summary>
        public event Action Changed;

        /// <summary>The scale content designed at this size gets in a space of this size.</summary>
        public float FitScale(Vector2 space)
        {
            float width = Mathf.Max(1, space.x - HorizontalMargin * 2), height = Mathf.Max(1, space.y - VerticalMargin * 2);
            float scale = Mathf.Max(.01f, Mathf.Min(width / DesignWidth, height / MinHeight));
            return MaxScale > 0 ? Mathf.Min(scale, MaxScale) : scale;
        }

#if UNITY_EDITOR
        // Unity forbids resizing RectTransforms while it validates; the screen lays itself out on the next editor tick.
        private void OnValidate() => UnityEditor.EditorApplication.delayCall += () => { if (this != null) Changed?.Invoke(); };
#endif
    }
}
