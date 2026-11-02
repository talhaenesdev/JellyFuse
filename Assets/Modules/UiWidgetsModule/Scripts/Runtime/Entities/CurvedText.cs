using TMPro;
using UnityEngine;

namespace Modules.UiWidgetsModule.Entities
{
    /// <summary>
    /// Bends a TextMeshPro text along an arc, each letter lifted and turned to follow it - a title that
    /// curves over a ribbon. Curvature is how far the middle stands above the ends, in units; a negative
    /// value bends it down. Redraws whenever the text changes, in the Editor too.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CurvedText : MonoBehaviour
    {
        [Tooltip("How far the middle of the text stands above its ends, in units. Negative bends it down.")]
        [SerializeField] private float _curvature = 18;
        [Tooltip("Turns each letter to follow the arc; off keeps them upright.")]
        [SerializeField] private bool _rotateLetters = true;

        private TMP_Text _text;
        private float _shownCurvature = float.NaN;

        private void OnEnable()
        {
            _text = GetComponent<TMP_Text>();
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
            // Rebuilding the mesh raises the text-changed event, and the event bends it.
            _text.ForceMeshUpdate();
        }

        private void OnDisable()
        {
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
            // Unsubscribed first, so this rebuild puts the text back straight.
            if (_text != null) _text.ForceMeshUpdate();
        }

        private void OnValidate() => _shownCurvature = float.NaN;

        private void LateUpdate()
        {
            if (!Mathf.Approximately(_shownCurvature, _curvature)) _text.ForceMeshUpdate();
        }

        private void OnTextChanged(Object changed)
        {
            if (changed == _text) Bend();
        }

        private void Bend()
        {
            _shownCurvature = _curvature;
            var info = _text.textInfo;
            if (info == null || info.characterCount == 0) return;

            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                minX = Mathf.Min(minX, c.bottomLeft.x);
                maxX = Mathf.Max(maxX, c.topRight.x);
            }
            float half = (maxX - minX) * .5f;
            if (half <= 0) return;
            float centre = (maxX + minX) * .5f;

            for (int i = 0; i < info.characterCount; i++)
            {
                var c = info.characterInfo[i];
                if (!c.isVisible) continue;
                var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                int v = c.vertexIndex;
                Vector3 pivot = new((vertices[v].x + vertices[v + 2].x) * .5f, c.baseLine, 0);
                // A parabola through the ends, its top Curvature above them; its slope turns the letter.
                float t = (pivot.x - centre) / half;
                float lift = _curvature * (1 - t * t);
                float angle = _rotateLetters ? Mathf.Atan(-2 * _curvature * t / half) * Mathf.Rad2Deg : 0;
                var turn = Matrix4x4.TRS(new Vector3(0, lift, 0) + pivot, Quaternion.Euler(0, 0, angle), Vector3.one);
                for (int k = 0; k < 4; k++) vertices[v + k] = turn.MultiplyPoint3x4(vertices[v + k] - pivot);
            }
            _text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }
}
