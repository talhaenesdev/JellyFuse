#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Modules.ScreenModule.Editor
{
    /// <summary>
    /// Turns the screens over to a camera, so URP's Bloom reaches them: an overlay canvas is drawn after every
    /// camera and no post effect ever touches it. Under ScreenRoot it adds UICamera - the camera the
    /// ScreenManager's canvas draws through, with post-processing on - and a global Bloom volume, files the
    /// camera's URP data as the shared mono "UICamera" (MainModule switches Bloom off through it when the frame
    /// rate drops), and turns HDR on in the render pipeline assets. Running it again changes nothing.
    /// </summary>
    internal static class SetupRealBloom
    {
        private const string ROOT_PREFAB = "Assets/Modules/ScreenModule/Prefabs/ScreenRoot.prefab";
        private const string PROFILE = "Assets/Modules/ScreenModule/Scriptables/VP_Bloom.asset";
        private static readonly string[] PIPELINES = { "Assets/Settings/Mobile_RPAsset.asset", "Assets/Settings/PC_RPAsset.asset" };
        public const string CAMERA_NAME = "UICamera";

        [MenuItem("Tools/Wobble Fuse/Setup Real Bloom")]
        private static void Run()
        {
            var profile = Profile();
            var root = PrefabUtility.LoadPrefabContents(ROOT_PREFAB);
            try
            {
                var manager = root.GetComponentInChildren<Canvas>(true);
                if (manager == null)
                {
                    Debug.LogError("SetupRealBloom - ScreenRoot holds no ScreenManager canvas.");
                    return;
                }

                var cameraGo = root.transform.Find(CAMERA_NAME)?.gameObject ?? new GameObject(CAMERA_NAME);
                cameraGo.transform.SetParent(root.transform, false);
                cameraGo.transform.localPosition = new Vector3(0, 0, -100);
                var camera = Get<Camera>(cameraGo);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                // Every layer: the screens sit on Default and UI both, and nothing else is in front of this camera.
                camera.cullingMask = ~0;
                camera.nearClipPlane = .3f;
                camera.farClipPlane = 1000;
                camera.depth = 10;
                camera.allowHDR = true;
                camera.allowMSAA = false;
                var data = Get<UniversalAdditionalCameraData>(cameraGo);
                data.renderType = CameraRenderType.Base;
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.None;
                data.renderShadows = false;
                data.requiresDepthOption = CameraOverrideOption.Off;
                data.requiresColorOption = CameraOverrideOption.Off;

                // The canvas draws through the camera now, close in front of it.
                manager.renderMode = RenderMode.ScreenSpaceCamera;
                manager.worldCamera = camera;
                manager.planeDistance = 10;

                var volumeGo = root.transform.Find("BloomVolume")?.gameObject ?? new GameObject("BloomVolume");
                volumeGo.transform.SetParent(root.transform, false);
                var volume = Get<Volume>(volumeGo);
                volume.isGlobal = true;
                volume.priority = 10;
                volume.sharedProfile = profile;

                FileSharedMono(root, CAMERA_NAME, data);
                PrefabUtility.SaveAsPrefabAsset(root, ROOT_PREFAB);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            foreach (var path in PIPELINES)
            {
                var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
                if (pipeline == null) continue;
                pipeline.supportsHDR = true;
                EditorUtility.SetDirty(pipeline);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("SetupRealBloom - ScreenRoot draws its screens through UICamera with Bloom; HDR is on.");
        }

        /// <summary>The Bloom profile, made once and kept: its values are tuned in the Inspector afterwards.</summary>
        private static VolumeProfile Profile()
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PROFILE);
            if (profile != null) return profile;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(PROFILE));
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, PROFILE);
            var bloom = profile.Add<Bloom>(true);
            // Only what is close to white glows: the gold, the highlights, the sparkles - not every light panel.
            bloom.threshold.Override(.85f);
            bloom.intensity.Override(.8f);
            bloom.scatter.Override(.65f);
            bloom.highQualityFiltering.Override(false);
            bloom.maxIterations.Override(5);
            bloom.name = "Bloom";
            AssetDatabase.AddObjectToAsset(bloom, profile);
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        /// <summary>Files the component in the Root adapter's Shared Monos, replacing what was filed under the name.</summary>
        private static void FileSharedMono(GameObject root, string key, MonoBehaviour component)
        {
            foreach (var behaviour in root.GetComponents<MonoBehaviour>())
            {
                var field = FindField(behaviour.GetType(), "_sharedMonoMap");
                if (field == null) continue;
                var map = field.GetValue(behaviour) as SerializedDictionary<string, MonoBehaviour>;
                if (map == null)
                {
                    map = new SerializedDictionary<string, MonoBehaviour>();
                    field.SetValue(behaviour, map);
                }
                map[key] = component;
                map.OnBeforeSerialize();
                EditorUtility.SetDirty(behaviour);
                return;
            }
            Debug.LogError("SetupRealBloom - ScreenRoot has no RootAdapter to file UICamera on.");
        }

        // GetComponent hands back Unity's fake null in the Editor, which ?? does not see.
        private static T Get<T>(GameObject go) where T : Component
        {
            var found = go.GetComponent<T>();
            return found != null ? found : go.AddComponent<T>();
        }

        private static FieldInfo FindField(System.Type type, string name)
        {
            for (; type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field != null) return field;
            }
            return null;
        }
    }
}
#endif
