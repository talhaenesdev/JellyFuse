using System.Collections;
using TMPro;
using UnityEngine;
namespace Modules.GameplayModule.GameplayScreenModule.ViewsMediators
{
    /// <summary>
    /// The word a combo earns - Nice, Great, Amazing - popping up over the board and floating away. It is a copy
    /// of the warning line, so it wears the HUD's font, and one copy is kept and reused. It is coloured like the
    /// jelly whose fuse or door made the combo.
    /// </summary>
    public partial class GameplayScreenView
    {
        private TMP_Text _comboText;
        private Coroutine _comboShow;

        private void ShowCombo(int combo, Color jelly)
        {
            if (_message == null || !isActiveAndEnabled) return;
            if (_comboText == null)
            {
                _comboText = Instantiate(_message, _board);
                _comboText.name = "Txt_Combo";
                _comboText.raycastTarget = false;
                _comboText.enableWordWrapping = false;
                _comboText.alignment = TextAlignmentOptions.Center;
                var rect = _comboText.rectTransform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(900, 200);
            }
            _comboText.text = combo >= 4 ? L("gameplay.combo_4", "Amazing!") : combo == 3 ? L("gameplay.combo_3", "Great!") : L("gameplay.combo_2", "Nice!");
            // The jelly's own colour, opaque.
            var colour = jelly; colour.a = 1;
            _comboText.color = colour;
            _comboText.fontSize = 70 + Mathf.Min(combo, 5) * 8;
            _comboText.transform.SetAsLastSibling();
            if (_comboShow != null) StopCoroutine(_comboShow);
            _comboShow = StartCoroutine(FloatCombo());
        }

        private IEnumerator FloatCombo()
        {
            var rect = _comboText.rectTransform;
            _comboText.gameObject.SetActive(true);
            const float life = 1.1f;
            for (float t = 0; t < life; t += Time.unscaledDeltaTime)
            {
                float k = t / life;
                // Pops in big, settles, then drifts up and fades.
                float pop = k < .15f ? Mathf.Lerp(.4f, 1.2f, k / .15f) : Mathf.Lerp(1.2f, 1f, Mathf.Clamp01((k - .15f) / .15f));
                rect.localScale = Vector3.one * pop;
                rect.anchoredPosition = new Vector2(0, 120 + 140 * k);
                _comboText.alpha = k < .6f ? 1 : 1 - (k - .6f) / .4f;
                yield return null;
            }
            _comboText.gameObject.SetActive(false);
            _comboShow = null;
        }
    }
}
