using UnityEngine;
using UnityEngine.UI;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// A profile picture: the player's icon with a frame drawn over it. It shows what it is given and
    /// knows nothing about where icons and frames come from.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ProfileAvatar : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private Image _frame;

        public void SetIcon(Sprite sprite, Color color) => Apply(_icon, sprite, color);
        public void SetFrame(Sprite sprite, Color color) => Apply(_frame, sprite, color);

        /// <summary>Hidden when there is neither a picture nor a visible colour to show.</summary>
        private static void Apply(Image image, Sprite sprite, Color color)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.color = color;
            image.enabled = sprite != null || color.a > 0;
        }
    }
}
