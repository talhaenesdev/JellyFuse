#if UNITY_EDITOR
using System.Linq;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;

namespace Modules.GameplayModule.Editor
{
    /// <summary>
    /// One window for level work: the level list on the left, the selected level's designer on the
    /// right. Both sides are the same inspectors the assets show on their own, hosted here.
    /// </summary>
    public sealed class JellyLevelsWindow : EditorWindow
    {
        private const string WIDTH_PREF = "JellyFuse.LevelsWindow.ListWidth";
        private const float MIN_LIST_WIDTH = 240, MIN_LEVEL_WIDTH = 360;
        private float _listWidth = 360;
        private bool _resizing;
        [SerializeField] private CD_JellyLevel _level;
        private CD_JellyLevels _list;
        private CD_JellyLevelsEditor _listEditor;
        private UnityEditor.Editor _levelEditor;
        private Vector2 _listScroll, _levelScroll;
        private readonly EditorTexts _texts = new();

        [MenuItem("Floria/Gameplay/Levels", false, 0)]
        private static void Open()
        {
            var window = GetWindow<JellyLevelsWindow>("Levels");
            window.minSize = new Vector2(900, 500);
            window.Show();
        }

        private void OnEnable()
        {
            wantsMouseMove = true;
            _listWidth = EditorPrefs.GetFloat(WIDTH_PREF, 360);
            Undo.undoRedoPerformed += Repaint;
        }
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= Repaint;
            if (_listEditor != null) DestroyImmediate(_listEditor);
            if (_levelEditor != null) DestroyImmediate(_levelEditor);
        }

        private void FindList()
        {
            if (_list != null) return;
            var guid = AssetDatabase.FindAssets("t:CD_JellyLevels").FirstOrDefault();
            if (guid != null) _list = AssetDatabase.LoadAssetAtPath<CD_JellyLevels>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private void Select(CD_JellyLevel level)
        {
            _level = level;
            if (_levelEditor != null) DestroyImmediate(_levelEditor);
            _levelEditor = null;
            _levelScroll = Vector2.zero;
            GUI.FocusControl(null);
            Repaint();
        }

        /// <summary>The line between the two sides; drag it to change the list's width. The width is remembered.</summary>
        private void DrawSplitter()
        {
            var line = GUILayoutUtility.GetRect(4, 4, GUILayout.Width(4), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(new Rect(line.x + 1.5f, line.y, 1, line.height), new Color(0, 0, 0, _resizing ? .7f : .35f));
            var handle = new Rect(line.x - 3, line.y, line.width + 6, line.height);
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeHorizontal);
            var e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition)) { _resizing = true; e.Use(); }
            if (_resizing && e.type == EventType.MouseDrag)
            {
                _listWidth = Mathf.Clamp(e.mousePosition.x, MIN_LIST_WIDTH, Mathf.Max(MIN_LIST_WIDTH, position.width - MIN_LEVEL_WIDTH));
                e.Use(); Repaint();
            }
            if (_resizing && e.rawType == EventType.MouseUp) { _resizing = false; EditorPrefs.SetFloat(WIDTH_PREF, _listWidth); e.Use(); }
        }

        private void OnGUI()
        {
            FindList();
            if (_list == null)
            {
                EditorGUILayout.HelpBox(_texts.T("No CD_JellyLevels asset found. Create one with Create > Jelly Fuse > Level List."), MessageType.Warning);
                return;
            }
            if (_listEditor == null || _listEditor.target != _list)
            {
                if (_listEditor != null) DestroyImmediate(_listEditor);
                _listEditor = (CD_JellyLevelsEditor)UnityEditor.Editor.CreateEditor(_list, typeof(CD_JellyLevelsEditor));
            }
            _listEditor.OpenLevel = Select;
            if (_level == null && _list.Levels.Count > 0) _level = _list.Levels.FirstOrDefault(l => l != null);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.Width(_listWidth));
            _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
            _listEditor.OnInspectorGUI();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            DrawSplitter();

            EditorGUILayout.BeginVertical();
            if (_level == null) EditorGUILayout.HelpBox(_texts.T("Pick a level on the left, or add a new one."), MessageType.Info);
            else
            {
                if (_levelEditor == null || _levelEditor.target != _level) _levelEditor = UnityEditor.Editor.CreateEditor(_level);
                int number = _list.Levels.IndexOf(_level) + 1;
                EditorGUILayout.LabelField((number > 0 ? "Level " + number + "  ·  " : "") + _level.name, EditorStyles.largeLabel);
                _levelScroll = EditorGUILayout.BeginScrollView(_levelScroll);
                _levelEditor.OnInspectorGUI();
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // The level designer previews the placement under the cursor, so it needs every move.
            if (Event.current.type is EventType.MouseMove or EventType.MouseDrag) Repaint();
        }
    }
}
#endif
