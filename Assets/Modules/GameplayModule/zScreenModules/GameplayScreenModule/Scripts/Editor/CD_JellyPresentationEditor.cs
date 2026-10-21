#if UNITY_EDITOR
using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.GameplayScreenModule.ViewsMediators;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Editor
{
    /// <summary>
    /// What every gameplay-screen settings inspector shares: in Play Mode a change, or Refresh preview,
    /// redraws the boards that use the asset.
    /// </summary>
    public abstract class GameplayPreviewEditor : UnityEditor.Editor
    {
        /// <summary>The editor's text in the developer's language (LocalizationModule).</summary>
        protected readonly EditorTexts Texts = new();
        /// <summary>Whether the screen's settings use the asset this inspector shows.</summary>
        protected abstract bool Uses(CD_JellyPresentation presentation);
        /// <summary>Every field, labelled and explained in the developer's language; <paramref name="skip"/> leaves one out.</summary>
        protected virtual void DrawFields() => DrawTranslated(null, null);
        protected void DrawTranslated(System.Func<SerializedProperty, bool> skip, System.Action<SerializedProperty> after)
        {
            serializedObject.Update();
            var property = serializedObject.GetIterator();
            for (bool enter = true; property.NextVisible(enter); enter = false)
            {
                if (property.name == "m_Script" || (skip != null && skip(property))) continue;
                EditorGUILayout.PropertyField(property, Texts.Label(property), true);
                after?.Invoke(property);
            }
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawFields();
            bool changed = EditorGUI.EndChangeCheck();
            bool refresh = Application.isPlaying && GUILayout.Button(Texts.T("Refresh preview"));
            if (!Application.isPlaying || !(changed || refresh)) return;
            foreach (var view in Object.FindObjectsByType<GameplayScreenView>(FindObjectsSortMode.None))
                if (view.Presentation != null && Uses(view.Presentation)) view.RefreshPresentation();
        }
    }

    /// <summary>The screen's settings in one place: the Feel, Look and Hud assets, each shown below its slot.</summary>
    [CustomEditor(typeof(CD_JellyPresentation))]
    public sealed class CD_JellyPresentationEditor : UnityEditor.Editor
    {
        private readonly EditorTexts _texts = new();
        private readonly Dictionary<string, UnityEditor.Editor> _editors = new();
        private readonly Dictionary<string, bool> _open = new();

        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(_texts.T("Each part is an asset of its own. Edit it here or open it on its own; changes preview immediately in Play Mode."), MessageType.Info);
            serializedObject.Update();
            Part("Feel", "How the jellies move and react");
            Part("Look", "How the board looks: art, shading, colours, walls");
            Part("Hud", "The screen around the board");
            serializedObject.ApplyModifiedProperties();
        }
        private void Part(string field, string help)
        {
            var property = serializedObject.FindProperty(field);
            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(property, new GUIContent(_texts.T(field), _texts.T(help)));
            var asset = property.objectReferenceValue;
            if (asset == null) { EditorGUILayout.HelpBox(string.Format(_texts.T("Assign a {0} asset: the screen does not open without it."), field), MessageType.Error); return; }
            _open.TryGetValue(field, out bool open);
            open = EditorGUILayout.Foldout(open, _texts.T(help), true);
            _open[field] = open;
            if (!open) return;
            _editors.TryGetValue(field, out var editor);
            CreateCachedEditor(asset, null, ref editor);
            _editors[field] = editor;
            EditorGUI.indentLevel++;
            editor.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }
        private void OnDisable()
        {
            foreach (var editor in _editors.Values) if (editor != null) DestroyImmediate(editor);
            _editors.Clear();
        }
    }

    [CustomEditor(typeof(CD_JellyFeel))]
    public sealed class CD_JellyFeelEditor : GameplayPreviewEditor
    {
        // What AnimateJellies switches off, hidden with it so the inspector shows only what still does something.
        private readonly HashSet<string> _motion = new()
        {
            "NeighbourWobble", "JellyStiffness", "JellyDamping", "MoveStretch", "MaxStretch", "PullStretch",
            "GrabPop", "LandSquash", "FusePop", "BreathAmount", "BreathSpeed",
        };
        protected override bool Uses(CD_JellyPresentation presentation) => presentation.Feel == target;
        protected override void DrawFields()
        {
            bool animate = serializedObject.FindProperty("AnimateJellies").boolValue;
            bool grid = serializedObject.FindProperty("GridDrag").boolValue;
            DrawTranslated(p => (!animate && _motion.Contains(p.name)) || (p.name == "DragFollow" && grid), null);
        }
    }

    [CustomEditor(typeof(CD_JellyBoardLook))]
    public sealed class CD_JellyBoardLookEditor : GameplayPreviewEditor
    {
        protected override bool Uses(CD_JellyPresentation presentation) => presentation.Look == target;
        protected override void DrawFields() =>
            DrawTranslated(null, p => { if (p.name == "Palette") DrawPalette(p.objectReferenceValue as CD_JellyPalette); });

        /// <summary>The palette's colours, editable in place: the asset stays the one source both editors and the game read.</summary>
        private void DrawPalette(CD_JellyPalette palette)
        {
            if (palette == null) { EditorGUILayout.HelpBox(Texts.T("Assign CD_JellyPalette to edit the jelly colours here."), MessageType.Warning); return; }
            var data = new SerializedObject(palette);
            data.Update();
            var colours = data.FindProperty("Colours");
            EditorGUI.indentLevel++;
            for (int i = 0; i < colours.arraySize; i++)
            {
                var entry = colours.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Name"), GUIContent.none, GUILayout.MinWidth(80));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Tint"), GUIContent.none, GUILayout.MinWidth(80));
                EditorGUILayout.PropertyField(entry.FindPropertyRelative("Sprite"), GUIContent.none, GUILayout.MinWidth(80));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
            if (data.ApplyModifiedProperties()) GUI.changed = true;
        }
    }

    [CustomEditor(typeof(CD_GameplayHud))]
    public sealed class CD_GameplayHudEditor : GameplayPreviewEditor
    {
        protected override bool Uses(CD_JellyPresentation presentation) => presentation.Hud == target;
    }
}
#endif
