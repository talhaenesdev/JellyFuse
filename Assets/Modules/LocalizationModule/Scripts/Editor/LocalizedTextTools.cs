#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FlowIoC.BaseModule.Injectable.Components;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.LocalizationModule.Shared.Data.ValueObjects;
using Modules.LocalizationModule.Shared.Enums;
using Modules.LocalizationModule.ViewsMediators;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// What the text inspector and the texts panel share: the game's tables, finding a key, making one for a text,
    /// writing a translation, and turning a plain TextMeshPro text into a localized one.
    /// </summary>
    internal sealed class LocalizedTextTools
    {
        /// <summary>The game's tables - the ones the game reads, not the editor's own labels.</summary>
        public List<CD_LocalizationTable> Tables() => EditorTexts.Tables(LocalizationUsage.Game);

        /// <summary>Where a new key goes unless one is picked: the game table holding the most texts.</summary>
        public CD_LocalizationTable DefaultTable() =>
            Tables().Where(t => t != null).OrderByDescending(t => t.Texts.Count).FirstOrDefault();

        public CD_Localization Config() => EditorTexts.Localization();

        public IReadOnlyList<LanguageCVO> Languages()
        {
            var config = Config();
            return config != null ? config.Languages : new List<LanguageCVO>();
        }

        public string Source() => Config() != null ? Config().SourceLanguage : "en";

        /// <summary>The table and entry that hold the key, or nulls when no game table does.</summary>
        public (CD_LocalizationTable table, LocalizedTextCVO text) Find(string key)
        {
            if (string.IsNullOrEmpty(key)) return (null, null);
            foreach (var table in Tables())
            {
                var text = table != null ? table.Find(key) : null;
                if (text != null) return (table, text);
            }
            return (null, null);
        }

        /// <summary>Keys, and the source text beside each, whose key or any text holds the search - for the key picker.</summary>
        public List<(string key, string text)> Search(string search, int max)
        {
            var found = new List<(string, string)>();
            if (string.IsNullOrWhiteSpace(search)) return found;
            string needle = search.Trim().ToLowerInvariant();
            string source = Source();
            foreach (var table in Tables())
            {
                if (table == null) continue;
                foreach (var text in table.Texts)
                {
                    bool hit = text.Key != null && text.Key.ToLowerInvariant().Contains(needle)
                               || text.Translations.Any(t => t.Text != null && t.Text.ToLowerInvariant().Contains(needle));
                    if (!hit) continue;
                    found.Add((text.Key, text.In(source) ?? ""));
                    if (found.Count >= max) return found;
                }
            }
            return found;
        }

        /// <summary>A key already holding exactly this text in the source language - so the same words share one key.</summary>
        public string KeyHolding(string sourceText)
        {
            if (string.IsNullOrWhiteSpace(sourceText)) return null;
            string source = Source();
            foreach (var table in Tables())
            {
                if (table == null) continue;
                foreach (var text in table.Texts)
                    if (string.Equals(text.In(source), sourceText.Trim(), System.StringComparison.Ordinal)) return text.Key;
            }
            return null;
        }

        /// <summary>
        /// A new key for a text in a prefab or scene: the prefab's name and the object's, lower case with dots -
        /// settingsscreen.txt_title - numbered when it is taken.
        /// </summary>
        public string MakeKey(Component text)
        {
            string owner = text.transform.root != null ? text.transform.root.name : "ui";
            var prefab = PrefabUtility.GetNearestPrefabInstanceRoot(text);
            if (prefab != null) owner = prefab.name;
            var stage = UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(text.gameObject);
            if (stage != null) owner = Path.GetFileNameWithoutExtension(stage.assetPath);
            string key = Clean(owner) + "." + Clean(text.gameObject.name);
            string unique = key;
            for (int i = 2; Find(unique).text != null; i++) unique = key + "_" + i;
            return unique;
        }

        private static string Clean(string name)
        {
            var builder = new StringBuilder();
            foreach (char c in name.ToLowerInvariant())
                builder.Append(char.IsLetterOrDigit(c) ? c : '_');
            return Regex.Replace(builder.ToString(), "_+", "_").Trim('_');
        }

        /// <summary>Writes one language's text for a key, adding the key to the table when it is new. Undoable.</summary>
        public void Write(CD_LocalizationTable table, string key, string code, string value)
        {
            if (table == null || string.IsNullOrEmpty(key)) return;
            Undo.RecordObject(table, "Edit text");
            table.FindOrAdd(key).Set(code, value);
            EditorUtility.SetDirty(table);
        }

        /// <summary>
        /// Whether a screen's code fills the text: some other component of the prefab or scene holds a reference to it
        /// (a View's serialized field). A localized text there would fight the code for the same words.
        /// </summary>
        public bool FilledByCode(TMP_Text text)
        {
            var root = text.transform.root;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null || component == text || component is LocalizedTextView || component is TMP_Text) continue;
                var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.NextVisible(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    var value = property.objectReferenceValue;
                    if (value == text || value == text.gameObject || value == text.rectTransform) return true;
                    // Code often fills a button's label by looking inside the button it holds.
                    if (LabelOf(value) == text) return true;
                }
            }
            return false;
        }

        /// <summary>The one text inside a referenced object, two levels down at most - the label code reaches through it.</summary>
        private static TMP_Text LabelOf(Object value)
        {
            Transform holder = value switch
            {
                GameObject go => go.transform,
                Component component => component.transform,
                _ => null,
            };
            if (holder == null) return null;
            var texts = holder.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length != 1) return null;
            int depth = 0;
            for (var t = texts[0].transform; t != null && t != holder; t = t.parent) depth++;
            return depth <= 2 ? texts[0] : null;
        }

        /// <summary>A text the tools may localize: it holds words, and no screen code fills it.</summary>
        public bool IsFixed(TMP_Text text) =>
            text.GetComponent<LocalizedTextView>() == null && HasWords(text.text) && !FilledByCode(text);

        /// <summary>Whether a text is worth translating: words, not a number, a placeholder or empty.</summary>
        public bool HasWords(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            string plain = Regex.Replace(text, "<[^>]+>", "");
            return plain.Any(char.IsLetter) && plain.Trim().Length > 1;
        }

        /// <summary>
        /// Turns a plain TextMeshPro text into a localized one: adds the view, and points it at a key - the one already
        /// holding the same words, or a new one carrying the text as it reads now, in the source language.
        /// </summary>
        public LocalizedTextView Localize(TMP_Text text, CD_LocalizationTable table)
        {
            var view = text.GetComponent<LocalizedTextView>();
            if (view != null) return view;
            if (text.GetComponent<ViewInjector>() == null) Undo.AddComponent<ViewInjector>(text.gameObject);
            view = Undo.AddComponent<LocalizedTextView>(text.gameObject);
            string key = KeyHolding(text.text);
            if (key == null && table != null)
            {
                key = MakeKey(text);
                Write(table, key, Source(), text.text);
            }
            Undo.RecordObject(view, "Localize text");
            view.Key = key;
            view.PointAtService();
            EditorUtility.SetDirty(view);
            EditorUtility.SetDirty(text.GetComponent<ViewInjector>());
            return view;
        }

        /// <summary>Shows the key's text in a language on the text itself, so the prefab reads as the game will.</summary>
        public void Preview(LocalizedTextView view, string code)
        {
            var (_, entry) = Find(view.Key);
            string value = entry?.In(code) ?? entry?.In(Source());
            if (value == null || view.Text == null || view.Text.text == value) return;
            Undo.RecordObject(view.Text, "Preview text");
            view.Text.text = value;
            EditorUtility.SetDirty(view.Text);
        }

        /// <summary>The code files that ask for the key by name - where a text the game fills in comes from.</summary>
        public List<string> CodeUsing(string key)
        {
            var files = new List<string>();
            if (string.IsNullOrEmpty(key)) return files;
            string quoted = "\"" + key + "\"";
            foreach (string path in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                if (File.ReadAllText(path).Contains(quoted))
                    files.Add("Assets" + path.Substring(Application.dataPath.Length).Replace('\\', '/'));
            }
            return files;
        }
    }
}
#endif
