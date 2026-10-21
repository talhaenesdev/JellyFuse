#if UNITY_EDITOR
using System.Collections.Generic;
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using Modules.GameplayModule.Shared.Enums;
using Modules.LocalizationModule.Shared.Enums;
using UnityEditor;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.Editor
{
    /// <summary>
    /// The info panel's pages, each text written in every language the game offers. A text's key names its entry
    /// in the chosen localization table; what is typed in a language is written to that entry, and the source
    /// language's text is also kept on the page, shown when the key has no translation.
    /// </summary>
    [CustomEditor(typeof(CD_InfoPopups))]
    public sealed class CD_InfoPopupsEditor : UnityEditor.Editor
    {
        private readonly EditorTexts _texts = new();
        private readonly HashSet<int> _open = new();
        private string T(string english) => _texts.T(english);

        public override void OnInspectorGUI()
        {
            var popups = (CD_InfoPopups)target;
            var localization = EditorTexts.Localization();
            EditorGUI.BeginChangeCheck();
            var table = (CD_LocalizationTable)EditorGUILayout.ObjectField(new GUIContent(T("Localization table"),
                T("The table the pages' texts are written to. The game reads them by key from every table.")), popups.Table, typeof(CD_LocalizationTable), false);
            if (EditorGUI.EndChangeCheck()) { Undo.RecordObject(popups, "Info table"); popups.Table = table; EditorUtility.SetDirty(popups); }
            if (popups.Table == null)
                EditorGUILayout.HelpBox(T("Pick a Game table to write the texts in every language."), MessageType.Info);
            else if (popups.Table.Usage != LocalizationUsage.Game)
                EditorGUILayout.HelpBox(T("This is an Editor table: the game does not read it. Pick a Game table."), MessageType.Warning);
            if (localization == null) EditorGUILayout.HelpBox(T("No CD_Localization in the project: only the English is edited."), MessageType.Info);

            for (int i = 0; i < popups.Pages.Count; i++)
            {
                var page = popups.Pages[i];
                if (page == null) continue;
                EditorGUILayout.Space(4);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                bool open = EditorGUILayout.Foldout(_open.Contains(i), string.IsNullOrEmpty(page.Key) ? T("(no key)") : page.Key, true, EditorStyles.foldoutHeader);
                if (open) _open.Add(i); else _open.Remove(i);
                if (GUILayout.Button(T("Delete"), GUILayout.Width(60)))
                {
                    Change(popups, "Delete info page", () => popups.Pages.RemoveAt(i));
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
                if (open) DrawPage(popups, page, localization);
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.Space(6);
            if (GUILayout.Button(T("+ Add page"), GUILayout.Height(24)))
            {
                Change(popups, "Add info page", () => popups.Pages.Add(new InfoPopupCVO { Key = "page" + (popups.Pages.Count + 1) }));
                _open.Add(popups.Pages.Count - 1);
            }
        }

        private void DrawPage(CD_InfoPopups popups, InfoPopupCVO page, CD_Localization localization)
        {
            EditorGUI.BeginChangeCheck();
            string key = EditorGUILayout.TextField(new GUIContent(T("Key"), T("What asks for this page: a booster's Info Key, or any other feature's.")), page.Key);
            var image = (Sprite)EditorGUILayout.ObjectField(T("Image"), page.Image, typeof(Sprite), false);
            var size = EditorGUILayout.Vector2Field(new GUIContent(T("Image size"), T("The picture's size on the panel, in canvas units. 0: as the panel lays it out.")), page.ImageSize);
            bool lead = EditorGUILayout.Toggle(new GUIContent(T("Lead to booster"), T("On: once the page is closed, the player is led to use this booster - everything else is dark until it is used.")), page.LeadToBooster);
            var booster = page.Booster;
            if (lead) booster = (BoosterType)EditorGUILayout.EnumPopup(T("Booster"), page.Booster);
            if (EditorGUI.EndChangeCheck()) Change(popups, "Info page", () => { page.Key = key; page.Image = image; page.ImageSize = size; page.LeadToBooster = lead; page.Booster = booster; });

            DrawText(popups, page, localization, "Title", "title", () => page.TitleKey, v => page.TitleKey = v, () => page.Title, v => page.Title = v, false);
            DrawText(popups, page, localization, "Subtitle", "subtitle", () => page.SubtitleKey, v => page.SubtitleKey = v, () => page.Subtitle, v => page.Subtitle = v, false);
            DrawText(popups, page, localization, "Description", "description", () => page.DescriptionKey, v => page.DescriptionKey = v, () => page.Description, v => page.Description = v, true);
            DrawText(popups, page, localization, "Button", "button", () => page.ButtonKey, v => page.ButtonKey = v, () => page.Button, v => page.Button = v, false);
        }

        /// <summary>One text of a page: its key, and what it says in each language, read from and written to the table.</summary>
        private void DrawText(CD_InfoPopups popups, InfoPopupCVO page, CD_Localization localization, string label, string part,
            System.Func<string> getKey, System.Action<string> setKey, System.Func<string> getEnglish, System.Action<string> setEnglish, bool long_)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(T(label), EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            string key = EditorGUILayout.TextField(new GUIContent(T("Key"), T("The text's localization key. Pages may share one: a change here changes it on all of them.")), getKey());
            if (EditorGUI.EndChangeCheck()) Change(popups, "Info text key", () => setKey(key.Trim()));
            if (string.IsNullOrEmpty(getKey()) && !string.IsNullOrEmpty(page.Key) && GUILayout.Button(T("Make key"), GUILayout.Width(80)))
                Change(popups, "Info text key", () => setKey("info." + page.Key + "." + part));
            EditorGUILayout.EndHorizontal();

            int shared = 0;
            foreach (var other in popups.Pages) if (other != null && !string.IsNullOrEmpty(getKey()) && Uses(other, getKey())) shared++;
            if (shared > 1) EditorGUILayout.LabelField(" ", string.Format(T("Shared by {0} pages."), shared), EditorStyles.miniLabel);

            string source = localization != null ? localization.SourceLanguage : "en";
            var entry = popups.Table != null && !string.IsNullOrEmpty(getKey()) ? popups.Table.Find(getKey()) : null;
            var languages = localization != null ? localization.Languages : null;
            if (languages == null || languages.Count == 0 || popups.Table == null || string.IsNullOrEmpty(getKey()))
            {
                // No table or no key: only the text kept on the page.
                EditorGUI.BeginChangeCheck();
                string english = TextField(T("Text"), getEnglish(), long_);
                if (EditorGUI.EndChangeCheck()) Change(popups, "Info text", () => setEnglish(english));
            }
            else
                foreach (var language in languages)
                {
                    string current = entry?.In(language.Code) ?? (language.Code == source ? getEnglish() : null) ?? "";
                    EditorGUI.BeginChangeCheck();
                    string text = TextField(string.IsNullOrEmpty(language.Name) ? language.Code : language.Name, current, long_);
                    if (!EditorGUI.EndChangeCheck()) continue;
                    var table = popups.Table;
                    Undo.RecordObjects(new Object[] { popups, table }, "Info text");
                    table.FindOrAdd(getKey()).Set(language.Code, text);
                    EditorUtility.SetDirty(table);
                    // The source language is also kept on the page, for a key that has no translation.
                    if (language.Code == source) { setEnglish(text); EditorUtility.SetDirty(popups); }
                }
            if (popups.Table != null && !string.IsNullOrEmpty(getKey()) && entry == null)
                EditorGUILayout.LabelField(" ", T("Not in the table yet: typing a text adds it."), EditorStyles.miniLabel);
            EditorGUI.indentLevel--;
        }
        private static string TextField(string label, string value, bool long_)
        {
            if (!long_) return EditorGUILayout.TextField(label, value);
            EditorGUILayout.LabelField(label);
            return EditorGUILayout.TextArea(value ?? "", EditorStyles.textArea, GUILayout.MinHeight(48));
        }
        private static bool Uses(InfoPopupCVO page, string key) =>
            page.TitleKey == key || page.SubtitleKey == key || page.DescriptionKey == key || page.ButtonKey == key;
        private static void Change(CD_InfoPopups popups, string label, System.Action action)
        {
            Undo.RecordObject(popups, label);
            action();
            EditorUtility.SetDirty(popups);
        }
    }
}
#endif
