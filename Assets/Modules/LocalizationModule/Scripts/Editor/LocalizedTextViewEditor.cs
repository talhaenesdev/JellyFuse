#if UNITY_EDITOR
using System.Linq;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.ViewsMediators;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// The text's inspector: its key, found by searching, and every language's words for it, edited in place and
    /// written straight into the table. A key that does not exist yet is made from the text as it reads.
    /// </summary>
    [CustomEditor(typeof(LocalizedTextView))]
    [CanEditMultipleObjects]
    internal sealed class LocalizedTextViewEditor : UnityEditor.Editor
    {
        private const string PREVIEW_PREF = "Localization.PreviewLanguage";

        private readonly LocalizedTextTools _tools = new();
        private string _search = "";
        private int _tableIndex = -1;

        public override void OnInspectorGUI()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.HelpBox("Select one text to edit its words.", MessageType.Info);
                return;
            }
            var view = (LocalizedTextView)target;
            var languages = _tools.Languages();
            if (_tools.Config() == null)
            {
                EditorGUILayout.HelpBox("No CD_Localization was found in the project.", MessageType.Warning);
                return;
            }

            // ---- the key ----
            EditorGUI.BeginChangeCheck();
            string key = EditorGUILayout.DelayedTextField(new GUIContent("Key", "The text's key in the localization tables."), view.Key);
            if (EditorGUI.EndChangeCheck()) SetKey(view, key);

            var (table, entry) = _tools.Find(view.Key);

            if (entry == null)
            {
                DrawMissing(view);
                return;
            }

            EditorGUILayout.LabelField("Table", table.name, EditorStyles.miniLabel);

            // ---- the words, in every language ----
            EditorGUILayout.Space(4);
            string source = _tools.Source();
            foreach (var language in languages)
            {
                string value = entry.In(language.Code) ?? "";
                EditorGUILayout.LabelField(language.Name + (language.Code == source ? "  (source)" : ""), EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                string edited = EditorGUILayout.TextArea(value, GUILayout.MinHeight(36));
                if (EditorGUI.EndChangeCheck())
                {
                    _tools.Write(table, view.Key, language.Code, edited);
                    _tools.Preview(view, PreviewCode(languages.Select(l => l.Code).ToArray()));
                }
                if (string.IsNullOrEmpty(edited))
                    EditorGUILayout.HelpBox($"No {language.Name} text yet: the game shows the source language's.", MessageType.Warning);
            }

            // ---- which language the prefab shows ----
            EditorGUILayout.Space(6);
            var codes = languages.Select(l => l.Code).ToArray();
            var names = languages.Select(l => l.Name).ToArray();
            int current = Mathf.Max(0, System.Array.IndexOf(codes, PreviewCode(codes)));
            int picked = EditorGUILayout.Popup(new GUIContent("Show in editor", "The language this text is shown in while editing. The game picks its own."), current, names);
            if (picked != current) EditorPrefs.SetString(PREVIEW_PREF, codes[picked]);
            _tools.Preview(view, codes.Length > 0 ? codes[picked] : source);

            // ---- other places using the same words ----
            var code = _tools.CodeUsing(view.Key);
            if (code.Count > 0)
                EditorGUILayout.HelpBox("This key is also asked for in code:\n" + string.Join("\n", code), MessageType.None);

            EditorGUILayout.Space(4);
            DrawPicker(view, "Use another key");
        }

        private void DrawMissing(LocalizedTextView view)
        {
            var tables = _tools.Tables();
            if (tables.Count == 0)
            {
                EditorGUILayout.HelpBox("There is no game table to write to. Add one to CD_Localization.", MessageType.Warning);
                return;
            }
            EditorGUILayout.HelpBox(string.IsNullOrEmpty(view.Key)
                ? "This text has no key yet. Make one from its words, or pick an existing one."
                : $"No table has the key \"{view.Key}\". Make it, or pick an existing one.", MessageType.Info);

            if (_tableIndex < 0) _tableIndex = Mathf.Max(0, tables.IndexOf(_tools.DefaultTable()));
            _tableIndex = Mathf.Clamp(_tableIndex, 0, tables.Count - 1);
            _tableIndex = EditorGUILayout.Popup("Table", _tableIndex, tables.Select(t => t.name).ToArray());
            if (GUILayout.Button(string.IsNullOrEmpty(view.Key) ? "Make a key from this text" : $"Make \"{view.Key}\""))
            {
                string key = string.IsNullOrEmpty(view.Key) ? _tools.MakeKey(view) : view.Key;
                _tools.Write(tables[_tableIndex], key, _tools.Source(), view.Text != null ? view.Text.text : "");
                SetKey(view, key);
            }
            EditorGUILayout.Space(4);
            DrawPicker(view, "Pick an existing key");
        }

        private void DrawPicker(LocalizedTextView view, string label)
        {
            _search = EditorGUILayout.TextField(new GUIContent(label, "Type part of a key or of its words."), _search);
            foreach (var (key, text) in _tools.Search(_search, 8))
            {
                string shown = text.Length > 40 ? text.Substring(0, 40) + "…" : text;
                if (GUILayout.Button(new GUIContent(key + "   " + shown, text), EditorStyles.miniButton))
                {
                    SetKey(view, key);
                    _search = "";
                    GUI.FocusControl(null);
                }
            }
        }

        private void SetKey(LocalizedTextView view, string key)
        {
            Undo.RecordObject(view, "Change text key");
            view.Key = key;
            EditorUtility.SetDirty(view);
        }

        private static string PreviewCode(string[] codes)
        {
            string code = EditorPrefs.GetString(PREVIEW_PREF, codes.Length > 0 ? codes[0] : "en");
            return codes.Contains(code) ? code : codes.Length > 0 ? codes[0] : "en";
        }

        /// <summary>Right-click a TextMeshPro text: make it a localized text.</summary>
        [MenuItem("CONTEXT/TMP_Text/Localize this text")]
        private static void LocalizeText(MenuCommand command)
        {
            var tools = new LocalizedTextTools();
            var table = tools.DefaultTable();
            var view = tools.Localize((TMP_Text)command.context, table);
            Selection.activeObject = view;
        }
    }
}
#endif
