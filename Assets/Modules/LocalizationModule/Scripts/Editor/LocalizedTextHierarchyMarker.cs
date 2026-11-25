#if UNITY_EDITOR
using Modules.LocalizationModule.ViewsMediators;
using UnityEditor;
using UnityEngine;

namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// A small tag at the right of the Hierarchy on every localized text, showing its key: a screen's editable texts
    /// stand out at a glance. Typing t:LocalizedTextView in the Hierarchy's search lists only them.
    /// </summary>
    [InitializeOnLoad]
    internal static class LocalizedTextHierarchyMarker
    {
        private static GUIStyle _style;

        static LocalizedTextHierarchyMarker() => EditorApplication.hierarchyWindowItemOnGUI += Draw;

        private static void Draw(int instanceId, Rect row)
        {
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null || !go.TryGetComponent(out LocalizedTextView view)) return;

            _style ??= new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.35f, 0.75f, 0.95f) },
            };
            string label = string.IsNullOrEmpty(view.Key) ? "⚑ no key" : "⚑ " + view.Key;
            var size = _style.CalcSize(new GUIContent(label));
            var rect = new Rect(row.xMax - size.x - 4, row.y, size.x, row.height);
            GUI.Label(rect, new GUIContent(label, "Localized text - select it to edit its words in every language."), _style);
        }
    }
}
#endif
