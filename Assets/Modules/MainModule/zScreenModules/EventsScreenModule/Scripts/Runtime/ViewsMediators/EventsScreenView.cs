using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public class EventsScreenView : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _eventList;

        public Action BackClicked;

        /// <summary>
        /// A screen is pooled: hiding it deactivates the GameObject and shows the same instance
        /// again next time, so Awake and Start run once no matter how often the screen opens.
        /// Wire the buttons here and drop the listeners in OnDisable, or the second open leaves
        /// the first open's listeners attached.
        /// </summary>
        private void OnEnable()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
        }

        private void OnDisable()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_eventList != null) _eventList.verticalNormalizedPosition = 1f;
        }

        private void OnBackClicked() => BackClicked?.Invoke();

        /// <summary>
        /// This method runs if screenData.HasShowAnimation bool is true.
        /// If you don't use custom animations delete this method.
        /// </summary>
        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);

        /// <summary>
        /// This method runs if screenData.HasHideAnimation bool is true.
        /// If you don't use custom animations delete this method.
        /// </summary>
        protected override void PlayHideAnimation() => HideCompleted?.Invoke(this);
    }
}
