using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.EventsScreenModule.ViewsMediators
{
    [RequireComponent(typeof(ViewInjector))]
    public partial class EventsScreenView : ScreenView
    {
        [SerializeField] private Button _backButton;
        [SerializeField] private ScrollRect _eventList;
        [SerializeField] private Button _duelButton;

        public Action BackClicked;
        public Action DuelClicked;

        /// <summary>
        /// A screen is pooled: hiding it deactivates the GameObject and shows the same instance
        /// again next time, so Awake and Start run once no matter how often the screen opens.
        /// Wire the buttons here and drop the listeners in OnDisable, or the second open leaves
        /// the first open's listeners attached.
        /// </summary>
        private void OnEnable()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);
            if (_duelButton != null) _duelButton.onClick.AddListener(OnDuelClicked);
        }

        private void OnDisable()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);
            if (_duelButton != null) _duelButton.onClick.RemoveListener(OnDuelClicked);
        }

        public override void BeforeScreenActivation()
        {
            base.BeforeScreenActivation();
            if (_eventList != null) _eventList.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// The cards' words in the language in use: each card's name and line, its button, and the "coming soon" marks on
        /// the events not open yet. A key with no text keeps the English the prefab was authored with.
        /// </summary>
        public void ApplyTexts(Func<string, string> localize)
        {
            if (_eventList == null || localize == null) return;
            Transform content = _eventList.content;
            if (content == null) return;
            Text(content, "Pnl_Event_Duel/Txt_Name", "events.duel.name", localize);
            Text(content, "Pnl_Event_Duel/Btn_PlayDuel/Txt_Label", "events.play", localize);
            _localize = localize;
            Text(content, "Pnl_Event_StarRush/Txt_Name", "events.starrush.name", localize);
            if (_events != null) ApplyEvents(_events);
            Text(content, "Pnl_Event_ColorCarnival/Txt_Name", "events.colorcarnival.name", localize);
            Text(content, "Pnl_Event_SpeedRun/Txt_Name", "events.speedrun.name", localize);
            foreach (string key in new string[0])
            {
                string card = "Pnl_Event_" + key;
                string id = "events." + key.ToLowerInvariant();
                Text(content, card + "/Txt_Name", id + ".name", localize);
                Text(content, card + "/Txt_Description", id + ".desc", localize);
                Text(content, card + "/Btn_PlayEvent/Txt_Play", "events.soon_button", localize);
                Text(content, card + "/Pnl_Time/Txt_Time", "events.soon", localize);
                Text(content, card + "/Pnl_Reward/Txt_Reward", id + ".reward", localize);
            }
        }

        private static void Text(Transform root, string path, string key, Func<string, string> localize)
        {
            var text = root.Find(path)?.GetComponent<TMPro.TMP_Text>();
            if (text == null) return;
            string value = localize(key);
            if (!string.IsNullOrEmpty(value) && value != key) text.text = value;
        }

        private void OnBackClicked() => BackClicked?.Invoke();
        private void OnDuelClicked() => DuelClicked?.Invoke();

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
