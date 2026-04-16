using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Puzzle2048.Core
{
    public class Tile : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI label;

        public int Value { get; private set; }
        public Vector2Int GridPosition { get; set; }
        public bool Merged { get; set; }

        private static readonly Color[] TileColors = new Color[]
        {
            new Color(0.80f, 0.75f, 0.70f), // empty
            new Color(0.93f, 0.89f, 0.85f), // 2
            new Color(0.93f, 0.88f, 0.78f), // 4
            new Color(0.95f, 0.69f, 0.47f), // 8
            new Color(0.96f, 0.58f, 0.39f), // 16
            new Color(0.96f, 0.49f, 0.37f), // 32
            new Color(0.96f, 0.37f, 0.23f), // 64
            new Color(0.93f, 0.81f, 0.45f), // 128
            new Color(0.93f, 0.80f, 0.38f), // 256
            new Color(0.93f, 0.78f, 0.31f), // 512
            new Color(0.93f, 0.77f, 0.25f), // 1024
            new Color(0.93f, 0.76f, 0.18f), // 2048
        };

        public void SetValue(int value)
        {
            Value = value;
            label.text = value > 0 ? value.ToString() : "";
            int colorIndex = value > 0 ? Mathf.Clamp((int)Mathf.Log(value, 2), 0, TileColors.Length - 1) : 0;
            background.color = TileColors[colorIndex];
            label.color = value <= 4 ? new Color(0.47f, 0.43f, 0.40f) : Color.white;
        }

        public void AnimateSpawn()
        {
            transform.localScale = Vector3.zero;
            StartCoroutine(ScaleTo(Vector3.one, GameConfig.MERGE_ANIMATION_DURATION, easeOutBack: true));
        }

        public void AnimateMerge()
        {
            StartCoroutine(MergeSequence());
        }

        private IEnumerator ScaleTo(Vector3 target, float duration, bool easeOutBack = false)
        {
            Vector3 start = transform.localScale;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                float e = easeOutBack ? EaseOutBack(Mathf.Clamp01(t)) : Mathf.Clamp01(t);
                transform.localScale = Vector3.Lerp(start, target, e);
                yield return null;
            }
            transform.localScale = target;
        }

        private IEnumerator MergeSequence()
        {
            float half = GameConfig.MERGE_ANIMATION_DURATION * 0.5f;
            yield return StartCoroutine(ScaleTo(Vector3.one * 1.2f, half));
            yield return StartCoroutine(ScaleTo(Vector3.one, half));
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
