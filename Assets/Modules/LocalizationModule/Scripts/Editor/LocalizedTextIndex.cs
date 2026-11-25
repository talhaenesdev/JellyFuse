#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Modules.LocalizationModule.ViewsMediators;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Modules.LocalizationModule.Editor
{
    /// <summary>
    /// Where every text key is used: the prefabs whose LocalizedTextViews show it, and the code lines that ask for it.
    /// Built once when first searched and again on demand, so typing in the search stays quick.
    /// </summary>
    internal sealed class LocalizedTextIndex
    {
        /// <summary>A localized text in a prefab: the prefab, the object's path under its root, and the key.</summary>
        public readonly struct Place
        {
            public readonly string Prefab;
            public readonly string Path;
            public readonly string Key;

            public Place(string prefab, string path, string key)
            {
                Prefab = prefab;
                Path = path;
                Key = key;
            }
        }

        /// <summary>A code line asking for a key by name.</summary>
        public readonly struct CodeLine
        {
            public readonly string File;
            public readonly int Line;
            public readonly string Text;

            public CodeLine(string file, int line, string text)
            {
                File = file;
                Line = line;
                Text = text;
            }
        }

        private static readonly Regex KEY_IN_CODE = new("\"([a-z][a-z0-9_]*(?:\\.[a-z0-9_]+)+)\"", RegexOptions.Compiled);

        private readonly Dictionary<string, List<Place>> _places = new();
        private readonly Dictionary<string, List<CodeLine>> _code = new();

        public bool Built { get; private set; }
        public int PlaceCount { get; private set; }

        public void Build()
        {
            _places.Clear();
            _code.Clear();
            PlaceCount = 0;

            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                foreach (var view in prefab.GetComponentsInChildren<LocalizedTextView>(true))
                {
                    if (string.IsNullOrEmpty(view.Key)) continue;
                    Add(_places, view.Key, new Place(path, PathOf(view.transform, prefab.transform), view.Key));
                    PlaceCount++;
                }
            }

            foreach (string file in Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories))
            {
                string asset = "Assets" + file.Substring(Application.dataPath.Length).Replace('\\', '/');
                if (asset.Contains("/Editor/")) continue;
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                    foreach (Match match in KEY_IN_CODE.Matches(lines[i]))
                        Add(_code, match.Groups[1].Value, new CodeLine(asset, i + 1, lines[i].Trim()));
            }
            Built = true;
        }

        public IReadOnlyList<Place> PlacesOf(string key) =>
            key != null && _places.TryGetValue(key, out var list) ? list : (IReadOnlyList<Place>)System.Array.Empty<Place>();

        public IReadOnlyList<CodeLine> CodeOf(string key) =>
            key != null && _code.TryGetValue(key, out var list) ? list : (IReadOnlyList<CodeLine>)System.Array.Empty<CodeLine>();

        /// <summary>Opens the prefab in Prefab Mode and selects the text, so its inspector is right there.</summary>
        public void Open(Place place)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(place.Prefab);
            if (asset == null) return;
            AssetDatabase.OpenAsset(asset);
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage == null) return;
            var root = stage.prefabContentsRoot.transform;
            var target = string.IsNullOrEmpty(place.Path) ? root : root.Find(place.Path);
            if (target == null) return;
            Selection.activeGameObject = target.gameObject;
            EditorGUIUtility.PingObject(target.gameObject);
        }

        /// <summary>Opens the code file at the line that asks for the key.</summary>
        public void Open(CodeLine line)
        {
            var script = AssetDatabase.LoadAssetAtPath<MonoScript>(line.File);
            if (script != null) AssetDatabase.OpenAsset(script, line.Line);
        }

        private static string PathOf(Transform target, Transform root)
        {
            var names = new List<string>();
            for (var t = target; t != null && t != root; t = t.parent) names.Insert(0, t.name);
            return string.Join("/", names);
        }

        private static void Add<T>(Dictionary<string, List<T>> map, string key, T value)
        {
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<T>();
            list.Add(value);
        }
    }
}
#endif
