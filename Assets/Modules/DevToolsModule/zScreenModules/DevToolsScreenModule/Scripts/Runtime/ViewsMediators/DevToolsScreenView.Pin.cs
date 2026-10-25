using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Modules.DevToolsModule.DevToolsScreenModule.ViewsMediators
{
    public sealed partial class DevToolsScreenView
    {
        private const int MAX_PIN_LENGTH = 8;

        [Header("PIN pad")]
        [Tooltip("Covers the panel until the PIN is accepted. Shown when the panel was opened by the secret gesture.")]
        [SerializeField] private GameObject _pinPage;
        [SerializeField] private TMP_Text _pinTitle;
        [Tooltip("Shows one dot per digit typed.")]
        [SerializeField] private TMP_Text _pinDots;
        [Tooltip("Shows \"Wrong PIN\" after a PIN that did not match.")]
        [SerializeField] private TMP_Text _pinMessage;
        [Tooltip("The digit keys, 0 to 9 in that order.")]
        [SerializeField] private Button[] _pinDigits = new Button[10];
        [SerializeField] private Button _pinClear;
        [SerializeField] private Button _pinEnter;

        /// <summary>Enter was pressed: the digits typed.</summary>
        public Action<string> PinSubmitted;

        private string _pin = string.Empty;
        private readonly UnityEngine.Events.UnityAction[] _digitActions = new UnityEngine.Events.UnityAction[10];

        /// <summary>Covers the panel with the PIN pad, or takes it away.</summary>
        public void ShowPinPad(bool show)
        {
            _pin = string.Empty;
            if (_pinPage != null) _pinPage.SetActive(show);
            // The pad covers the panel; Close stays on top of it so a wrong guess can still leave.
            if (show && _closeButton != null) _closeButton.transform.SetAsLastSibling();
            if (_pinTitle != null) _pinTitle.text = L("devtools.pin_title", "Enter PIN");
            if (_pinMessage != null) _pinMessage.text = string.Empty;
            RefreshDots();
        }

        /// <summary>The PIN typed did not match: clears it and says so.</summary>
        public void ShowPinRejected()
        {
            _pin = string.Empty;
            RefreshDots();
            if (_pinMessage != null) _pinMessage.text = L("devtools.pin_wrong", "Wrong PIN");
        }

        private void WirePin(bool on)
        {
            for (int i = 0; i < _pinDigits.Length && i < 10; i++)
            {
                if (_pinDigits[i] == null) continue;
                if (on)
                {
                    string digit = i.ToString();
                    _digitActions[i] = () => TypeDigit(digit);
                    _pinDigits[i].onClick.AddListener(_digitActions[i]);
                }
                else if (_digitActions[i] != null) _pinDigits[i].onClick.RemoveListener(_digitActions[i]);
            }
            if (_pinClear != null) { if (on) _pinClear.onClick.AddListener(ClearPin); else _pinClear.onClick.RemoveListener(ClearPin); }
            if (_pinEnter != null) { if (on) _pinEnter.onClick.AddListener(SubmitPin); else _pinEnter.onClick.RemoveListener(SubmitPin); }
        }

        private void TypeDigit(string digit)
        {
            if (_pin.Length >= MAX_PIN_LENGTH) return;
            _pin += digit;
            if (_pinMessage != null) _pinMessage.text = string.Empty;
            RefreshDots();
        }

        private void ClearPin()
        {
            _pin = string.Empty;
            RefreshDots();
        }

        private void SubmitPin()
        {
            if (_pin.Length == 0) return;
            string typed = _pin;
            _pin = string.Empty;
            RefreshDots();
            PinSubmitted?.Invoke(typed);
        }

        private void RefreshDots()
        {
            if (_pinDots == null) return;
            _pinDots.text = _pin.Length == 0 ? "-" : new string('*', _pin.Length);
        }
    }
}
