#if UNITY_EDITOR
using Modules.LocalizationModule.Shared.Constants;
using Modules.LocalizationModule.Shared.Data.UnityObjects;
using UnityEditor;
using UnityEngine;
namespace Modules.LocalizationModule.Editor
{
    /// <summary>Draws [Section] as Unity draws [Header], in the developer's editor language.</summary>
    [CustomPropertyDrawer(typeof(SectionAttribute))]
    public sealed class SectionDrawer : DecoratorDrawer
    {
        private readonly EditorTexts _texts = new();

        public override float GetHeight() => EditorGUIUtility.singleLineHeight * 1.5f;

        public override void OnGUI(Rect position)
        {
            position.yMin += EditorGUIUtility.singleLineHeight * .5f;
            position = EditorGUI.IndentedRect(position);
            GUI.Label(position, _texts.T(((SectionAttribute)attribute).Title), EditorStyles.boldLabel);
        }
    }
}
#endif
