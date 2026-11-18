#if UNITY_ANDROID
using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace WobbleFuse.Editor.Android
{
    internal sealed class AndroidPortraitFeaturePostprocessor : IPostGenerateGradleAndroidProject
    {
        private const string ANDROID_NAMESPACE = "http://schemas.android.com/apk/res/android";
        private const string TOOLS_NAMESPACE = "http://schemas.android.com/tools";
        private const string XMLNS_NAMESPACE = "http://www.w3.org/2000/xmlns/";
        private const string PORTRAIT_FEATURE = "android.hardware.screen.portrait";

        public int callbackOrder => int.MaxValue;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            foreach (string manifestPath in GetManifestPaths(path))
            {
                MakePortraitFeatureOptional(manifestPath);
            }
        }

        private static IEnumerable<string> GetManifestPaths(string gradleProjectPath)
        {
            var manifests = new HashSet<string>();
            string projectRoot = Path.GetFullPath(gradleProjectPath);
            string parentRoot = Directory.GetParent(projectRoot)?.FullName;

            AddIfPresent(manifests, Path.Combine(projectRoot, "src", "main", "AndroidManifest.xml"));
            AddIfPresent(manifests, Path.Combine(projectRoot, "launcher", "src", "main", "AndroidManifest.xml"));
            AddIfPresent(manifests, Path.Combine(projectRoot, "unityLibrary", "src", "main", "AndroidManifest.xml"));

            if (!string.IsNullOrEmpty(parentRoot))
            {
                AddIfPresent(manifests, Path.Combine(parentRoot, "launcher", "src", "main", "AndroidManifest.xml"));
                AddIfPresent(manifests, Path.Combine(parentRoot, "unityLibrary", "src", "main", "AndroidManifest.xml"));
            }

            return manifests;
        }

        private static void AddIfPresent(ISet<string> manifests, string manifestPath)
        {
            if (File.Exists(manifestPath))
            {
                manifests.Add(Path.GetFullPath(manifestPath));
            }
        }

        private static void MakePortraitFeatureOptional(string manifestPath)
        {
            var document = new XmlDocument { PreserveWhitespace = true };
            document.Load(manifestPath);

            XmlElement manifest = document.DocumentElement;
            if (manifest == null)
            {
                return;
            }

            SetNamespacedAttribute(manifest, "xmlns", "tools", XMLNS_NAMESPACE, TOOLS_NAMESPACE);

            var namespaceManager = new XmlNamespaceManager(document.NameTable);
            namespaceManager.AddNamespace("android", ANDROID_NAMESPACE);

            XmlElement feature = document.SelectSingleNode(
                "/manifest/uses-feature[@android:name='" + PORTRAIT_FEATURE + "']",
                namespaceManager) as XmlElement;

            if (feature == null)
            {
                feature = document.CreateElement("uses-feature");
                SetNamespacedAttribute(feature, "android", "name", ANDROID_NAMESPACE, PORTRAIT_FEATURE);
                manifest.PrependChild(feature);
            }

            SetNamespacedAttribute(feature, "android", "required", ANDROID_NAMESPACE, "false");
            SetNamespacedAttribute(feature, "tools", "replace", TOOLS_NAMESPACE, "android:required");
            document.Save(manifestPath);
        }

        private static void SetNamespacedAttribute(
            XmlElement element,
            string prefix,
            string localName,
            string namespaceUri,
            string value)
        {
            XmlAttribute attribute = element.GetAttributeNode(localName, namespaceUri);
            if (attribute == null)
            {
                attribute = element.OwnerDocument.CreateAttribute(prefix, localName, namespaceUri);
                element.Attributes.Append(attribute);
            }

            attribute.Value = value;
        }
    }
}
#endif
