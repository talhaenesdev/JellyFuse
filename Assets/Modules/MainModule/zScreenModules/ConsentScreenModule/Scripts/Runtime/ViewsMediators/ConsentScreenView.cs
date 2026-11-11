using System;
using FlowIoC.BaseModule.Injectable.Components;
using FlowIoC.ScreenModule.ViewsMediators.Screen;
using Modules.UiWidgetsModule.Entities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.MainModule.ConsentScreenModule.ViewsMediators
{
    /// <summary>
    /// The consent question: a title, what the data is for, and two buttons - agree, or keep the ads
    /// unpersonalised. It has no close button, because leaving without an answer would be no answer at all.
    /// </summary>
    [RequireComponent(typeof(ViewInjector))]
    public sealed class ConsentScreenView : ScreenView
    {
        [Header("Texts")]
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _body;
        [SerializeField] private TMP_Text _acceptLabel;
        [SerializeField] private TMP_Text _declineLabel;

        [Header("Controls")]
        [SerializeField] private Button _acceptButton;
        [SerializeField] private Button _declineButton;

        public Action<bool> Answered;

        private Func<string, string> _localize;

        private void OnEnable()
        {
            _acceptButton.onClick.AddListener(OnAcceptClicked);
            _declineButton.onClick.AddListener(OnDeclineClicked);
        }

        private void OnDisable()
        {
            _acceptButton.onClick.RemoveListener(OnAcceptClicked);
            _declineButton.onClick.RemoveListener(OnDeclineClicked);
        }

        public void SetLocalizer(Func<string, string> localize)
        {
            _localize = localize;
            _title.text = L("consent.title", "Your Privacy");
            _body.text = L("consent.body",
                "We use your device's advertising ID and gameplay data to show ads that suit you and to make the game better. " +
                "You can refuse: you will still see ads, just not personalised ones.");
            _acceptLabel.text = L("consent.accept", "Accept");
            _declineLabel.text = L("consent.decline", "Refuse");
        }

        private string L(string key, string fallback)
        {
            string text = _localize?.Invoke(key);
            return string.IsNullOrEmpty(text) || text == key ? fallback : text;
        }

        private void OnAcceptClicked() => Answered?.Invoke(true);
        private void OnDeclineClicked() => Answered?.Invoke(false);

        protected override void PlayShowAnimation() => ShowCompleted?.Invoke(this);
        protected override void PlayHideAnimation()
        {
            var panel = GetComponentInChildren<PanelAnimator>();
            if (panel != null) panel.Close(() => HideCompleted?.Invoke(this));
            else HideCompleted?.Invoke(this);
        }
    }
}
