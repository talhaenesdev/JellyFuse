#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// The level list as a designer reads it: one numbered row per level, with the buttons to open,
    /// move, copy and remove it, and one button to add a new level at the end.
    /// Shown on its own in the inspector, and as the left side of the Levels window (Floria/Gameplay/Levels).
    /// </summary>
    [CustomEditor(typeof(CD_JellyLevels))]
    public sealed class CD_JellyLevelsEditor : UnityEditor.Editor
    {
        private bool _raw;
        // The editor's text in the developer's language (LocalizationModule). The English stays here.
        private readonly EditorTexts _texts = new();
        private string T(string english) => _texts.T(english);

        /// <summary>What Open, Add and Copy do with a level. The Levels window shows it beside the list; alone, the inspector selects it.</summary>
        public System.Action<CD_JellyLevel> OpenLevel;
        private void Open(CD_JellyLevel level)
        {
            if (OpenLevel != null) OpenLevel(level);
            else Selection.activeObject = level;
        }

        // A level's checks are run again only when the level was changed (its dirty count moved),
        // not on every repaint of the list.
        private readonly Dictionary<CD_JellyLevel, (int Dirty, bool Valid)> _validity = new();
        private bool IsValid(CD_JellyLevel level)
        {
            int dirty = EditorUtility.GetDirtyCount(level);
            if (_validity.TryGetValue(level, out var known) && known.Dirty == dirty) return known.Valid;
            // Walls left in doors are taken away here too, the way the level's own editor does.
            var walls = level.WallsInDoors();
            if (walls.Count > 0)
            {
                Undo.RecordObject(level, "Clear walls in doors");
                foreach (var wall in walls) level.Walls.Remove(wall);
                EditorUtility.SetDirty(level);
                dirty = EditorUtility.GetDirtyCount(level);
            }
            bool valid = level.Validate().Count == 0;
            _validity[level] = (dirty, valid);
            return valid;
        }

        public override void OnInspectorGUI()
        {
            var list = (CD_JellyLevels)target;
            EditorGUILayout.LabelField(T("JELLY • LEVELS"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(T("The order is the level number. Press Open to edit a level."), EditorStyles.wordWrappedMiniLabel);

            int start = EditorGUILayout.IntSlider(T("Start level"), list.StartLevel, 1, Mathf.Max(1, list.Count));
            if (start != list.StartLevel) Change(list, () => list.StartLevel = start);
            EditorGUILayout.Space(6);

            for (int i = 0; i < list.Levels.Count; i++)
            {
                var level = list.Levels[i];
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
                GUILayout.Label((i + 1).ToString(), style, GUILayout.Width(26), GUILayout.Height(22));
                if (level == null)
                    GUILayout.Label(T("(empty row)"), GUILayout.Height(22));
                else
                {
                    bool valid = IsValid(level);
                    GUILayout.Label(new GUIContent((valid ? "" : "⚠ ") + level.name + "  ·  " + level.Width + "×" + level.Height + "  ·  " + level.Seconds + " s",
                        valid ? "" : T("This level has errors; open it and read the warnings.")), GUILayout.Height(22));
                }
                if (list.StartLevel == i + 1) GUILayout.Label(T("▶ start"), EditorStyles.miniBoldLabel, GUILayout.Width(70));
                using (new EditorGUI.DisabledScope(level == null))
                    if (GUILayout.Button(T("Open"), GUILayout.Width(44))) Open(level);
                using (new EditorGUI.DisabledScope(i == 0))
                    if (GUILayout.Button("↑", GUILayout.Width(24))) { int index = i; Change(list, () => Swap(list, index, index - 1)); }
                using (new EditorGUI.DisabledScope(i == list.Levels.Count - 1))
                    if (GUILayout.Button("↓", GUILayout.Width(24))) { int index = i; Change(list, () => Swap(list, index, index + 1)); }
                using (new EditorGUI.DisabledScope(level == null))
                    if (GUILayout.Button(new GUIContent("⧉", T("Copy: the copy goes right after it")), GUILayout.Width(26)))
                    { Duplicate(list, i); GUIUtility.ExitGUI(); }
                if (GUILayout.Button(new GUIContent("✕", T("Remove from the list (the file is kept)")), GUILayout.Width(26)))
                { int index = i; Change(list, () => list.Levels.RemoveAt(index)); GUIUtility.ExitGUI(); }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(6);
            if (GUILayout.Button(T("+ Add new level"), GUILayout.Height(32))) { AddNew(list); GUIUtility.ExitGUI(); }
            var dropped = (CD_JellyLevel)EditorGUILayout.ObjectField(T("Add an existing level"), null, typeof(CD_JellyLevel), false);
            if (dropped != null)
            {
                if (list.Levels.Contains(dropped)) Debug.LogWarning(dropped.name + " is already in the level list.", list);
                else Change(list, () => list.Levels.Add(dropped));
            }

            EditorGUILayout.Space(6);
            _raw = EditorGUILayout.Foldout(_raw, T("Advanced: raw list"), true);
            if (_raw) DrawDefaultInspector();
        }

        private void Change(CD_JellyLevels list, System.Action action)
        {
            Undo.RecordObject(list, "Edit level list");
            action();
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssetIfDirty(list);
        }
        private static void Swap(CD_JellyLevels list, int a, int b) => (list.Levels[a], list.Levels[b]) = (list.Levels[b], list.Levels[a]);

        /// <summary>New levels sit beside the list asset, numbered after the highest level file there.</summary>
        private string NextPath(CD_JellyLevels list)
        {
            string folder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(list)).Replace('\\', '/');
            int number = AssetDatabase.FindAssets("t:CD_JellyLevel", new[] { folder })
                .Select(g => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g)))
                .Select(n => int.TryParse(n.Split('_').Last(), out int value) ? value : 0)
                .DefaultIfEmpty(0).Max() + 1;
            return AssetDatabase.GenerateUniqueAssetPath($"{folder}/CD_JellyLevel_{number:00}.asset");
        }

        private void AddNew(CD_JellyLevels list)
        {
            var level = CreateInstance<CD_JellyLevel>();
            string path = NextPath(list);
            level.LevelName = Path.GetFileNameWithoutExtension(path).Split('_').Last();
            AssetDatabase.CreateAsset(level, path);
            Undo.RegisterCreatedObjectUndo(level, "New level");
            Change(list, () => list.Levels.Add(level));
            Open(level);
        }

        private void Duplicate(CD_JellyLevels list, int index)
        {
            var source = list.Levels[index];
            AssetDatabase.SaveAssetIfDirty(source);
            string path = NextPath(list);
            if (!AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(source), path)) return;
            var copy = AssetDatabase.LoadAssetAtPath<CD_JellyLevel>(path);
            Change(list, () => list.Levels.Insert(index + 1, copy));
            Open(copy);
        }
    }
}
#endif
