#if UNITY_EDITOR
using Modules.GameplayModule.GameplayScreenModule.Data.UnityObjects;
using UnityEditor;
namespace Modules.GameplayModule.GameplayScreenModule.Editor
{
    /// <summary>The wall skin's inspector: in Play Mode every change redraws the board at once.</summary>
    [CustomEditor(typeof(CD_JellyWallSkin))]
    public sealed class CD_JellyWallSkinEditor : GameplayPreviewEditor
    {
        protected override bool Uses(CD_JellyPresentation presentation) => presentation.Look != null && presentation.Look.WallSkin == target;
        protected override void DrawFields()
        {
            EditorGUILayout.HelpBox(Texts.T("The walls and doors of the board. Changes preview immediately in Play Mode."), MessageType.Info);
            base.DrawFields();
        }
    }
}
#endif
