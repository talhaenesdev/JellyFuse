using System;
using Modules.GameplayModule.Shared.Enums;
using UnityEngine;

namespace Modules.GameplayModule.GameplayScreenModule.Data.ValueObjects
{
    /// <summary>
    /// One page of the info panel: a title, a subtitle under it, a picture in the middle and a description at the
    /// bottom, with a button. Each text is read by its localization key; the English beside it is shown when the
    /// key has no translation. Shown by its Key - a booster opening, a new kind of jelly, anything.
    /// </summary>
    [Serializable]
    public sealed class InfoPopupCVO
    {
        [Tooltip("What asks for this page: a booster's Info Key, or any other feature's.")]
        public string Key;
        public string TitleKey;
        public string Title = "New!";
        public string SubtitleKey;
        public string Subtitle;
        public Sprite Image;
        [Tooltip("The picture's size on the panel, in canvas units. 0: as the panel lays it out.")]
        public Vector2 ImageSize;
        public string DescriptionKey;
        [TextArea] public string Description;
        public string ButtonKey = "info.ok";
        public string Button = "OK";
        [Tooltip("On: once the page is closed, the player is led to use this booster - everything else is dark until it is used.")]
        public bool LeadToBooster;
        public BoosterType Booster;
    }
}
