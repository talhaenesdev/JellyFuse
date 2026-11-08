#if UNITY_EDITOR
using Modules.UiWidgetsModule.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Editor
{
    /// <summary>
    /// Gives every Button in the game's prefabs the press bounce - ButtonPressAnimator, with its default preset -
    /// where it is missing, so a newly authored screen gets the same feel as the rest. Test modules are left alone;
    /// a button that has one keeps its own. Running it again changes nothing.
    /// </summary>
    internal static class AddButtonPress
    {
        [MenuItem("Tools/Wobble Fuse/Add Button Press To All Buttons")]
        private static void Run()
        {
            int added = 0, prefabs = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Modules" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/zTestModules/")) continue;
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool needs = false;
                foreach (var button in asset.GetComponentsInChildren<Button>(true))
                    if (button.GetComponent<ButtonPressAnimator>() == null) { needs = true; break; }
                if (!needs) continue;

                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int here = 0;
                    foreach (var button in root.GetComponentsInChildren<Button>(true))
                    {
                        if (button.GetComponent<ButtonPressAnimator>() != null) continue;
                        // A button inside a nested prefab is that prefab's to change.
                        if (PrefabUtility.IsPartOfPrefabInstance(button.gameObject) && !PrefabUtility.IsOutermostPrefabInstanceRoot(button.gameObject)
                            && PrefabUtility.GetCorrespondingObjectFromSource(button.gameObject) != null) continue;
                        button.gameObject.AddComponent<ButtonPressAnimator>();
                        here++;
                    }
                    if (here > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        added += here; prefabs++;
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }
            Debug.Log($"AddButtonPress - {added} buttons in {prefabs} prefabs now bounce when pressed.");
        }
    }
}
#endif
