using System.Collections.Generic;
using Modules.UiWidgetsModule.Data.ValueObjects;
using UnityEngine;

namespace Modules.UiWidgetsModule.Data.UnityObjects
{
    /// <summary>
    /// A text animation: one or more effects that play together on a TextMeshPro text with a TextAnimator.
    /// Make one per look (Pop, Typewriter, Count Up...) and drag it onto any TextAnimator.
    /// </summary>
    [CreateAssetMenu(fileName = "CD_TextAnimation", menuName = "UI Widgets/Text Animation")]
    public sealed class CD_TextAnimation : ScriptableObject
    {
        public List<TextAnimationCVO> Effects = new() { new TextAnimationCVO() };
    }
}
