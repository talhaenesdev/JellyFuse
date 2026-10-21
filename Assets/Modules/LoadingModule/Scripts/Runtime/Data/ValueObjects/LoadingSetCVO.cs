using System;
using System.Collections.Generic;
using Modules.LoadingModule.Shared.Enums;
using UnityEngine;

namespace Modules.LoadingModule.Data.ValueObjects
{
    [Serializable]
    public class LoadingSetCVO
    {
        public string Key;
        public LoadingPresentation Presentation = LoadingPresentation.Fullscreen;

        [Tooltip("A running set that hears nothing for this long is reported once, naming the steps it is waiting for.")]
        public float StallWarningSeconds = 10f;

        [Tooltip("A fullscreen set's screen stays up at least this many seconds from when it opened, even when the set ends sooner.")]
        [Min(0)] public float MinimumScreenSeconds;

        public List<LoadingStepCVO> Steps = new();
    }
}
