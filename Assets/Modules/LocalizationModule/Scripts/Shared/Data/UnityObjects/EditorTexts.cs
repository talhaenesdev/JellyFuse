#if UNITY_EDITOR
using System.Collections.Generic;
using Modules.LocalizationModule.Shared.Enums;
using UnityEditor;
using UnityEngine;
namespace Modules.LocalizationModule.Shared.Data.UnityObjects
{
    /// <summary>
    /// The editor's side of localization: give it the English an editor would show and it returns the
    /// text in the developer's language, from the project's Editor tables, or the English itself.
    ///
    /// An editor holds its own instance. The language is kept per developer (EditorPrefs), so picking
    /// one changes no file in the project. While a language other than English is picked and
    /// collecting is on, an English text no Editor table has is added to the first one with no
    /// translation, so the table lists everything still to translate.
    /// </summary>
    public sealed class EditorTexts
    {
        public const string LANGUAGE_PREF = "Localization.EditorLanguage";
        public const string COLLECT_PREF = "Localization.CollectEditorTexts";
        public const string ENGLISH = "en";

        private string _code;
        private readonly List<CD_LocalizationTable> _tables = new();
        private int _dirty = -1;
        private readonly Dictionary<string, string> _map = new();

        /// <summary>The developer's editor language, by its code; "en" for English.</summary>
        public static string Code
        {
            get => EditorPrefs.GetString(LANGUAGE_PREF, ENGLISH);
            set => EditorPrefs.SetString(LANGUAGE_PREF, value);
        }

        /// <summary>The text in the developer's language; the English itself when there is none.</summary>
        public string T(string english)
        {
            if (string.IsNullOrEmpty(english) || !Refresh()) return english;
            if (_map.TryGetValue(english, out var text)) return string.IsNullOrEmpty(text) ? english : text;
            if (EditorPrefs.GetBool(COLLECT_PREF, true)) Collect(english);
            return english;
        }
        /// <summary>A field's label and tooltip, in the developer's language.</summary>
        public GUIContent Label(SerializedProperty property) => new(T(property.displayName), T(property.tooltip));
        public GUIContent Label(string english, string tooltip = null) => new(T(english), T(tooltip));

        /// <summary>Whether a language other than English is picked and an Editor table exists; rebuilds the lookup after an edit.</summary>
        private bool Refresh()
        {
            string code = Code;
            if (code == ENGLISH) { _code = code; _tables.Clear(); return false; }
            if (code != _code || _tables.Count == 0 || _tables.Exists(t => t == null))
            {
                _code = code; _dirty = -1;
                _tables.Clear();
                _tables.AddRange(Tables(LocalizationUsage.Editor));
                if (_tables.Count == 0) return false;
            }
            int dirty = 0;
            foreach (var table in _tables) dirty += EditorUtility.GetDirtyCount(table);
            if (dirty != _dirty)
            {
                _dirty = dirty;
                _map.Clear();
                foreach (var table in _tables)
                    foreach (var text in table.Texts)
                        if (!string.IsNullOrEmpty(text.Key) && !_map.ContainsKey(text.Key)) _map[text.Key] = text.In(code);
            }
            return true;
        }
        private void Collect(string english)
        {
            var table = _tables[0];
            table.FindOrAdd(english);
            _map[english] = null;
            EditorUtility.SetDirty(table);
            _dirty = -1;
        }

        /// <summary>Every table in the project used this way.</summary>
        public static List<CD_LocalizationTable> Tables(LocalizationUsage usage)
        {
            var list = new List<CD_LocalizationTable>();
            foreach (var guid in AssetDatabase.FindAssets("t:CD_LocalizationTable"))
            {
                var table = AssetDatabase.LoadAssetAtPath<CD_LocalizationTable>(AssetDatabase.GUIDToAssetPath(guid));
                if (table != null && table.Usage == usage) list.Add(table);
            }
            return list;
        }
        /// <summary>The project's CD_Localization, or null.</summary>
        public static CD_Localization Localization()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CD_Localization"))
            {
                var localization = AssetDatabase.LoadAssetAtPath<CD_Localization>(AssetDatabase.GUIDToAssetPath(guid));
                if (localization != null) return localization;
            }
            return null;
        }
    }
}
#endif
