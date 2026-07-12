using TMPro;
using UnityEngine;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private TMP_Text targetText;

    [Header("Blink")]
    [SerializeField] private float speed = 1.5f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1f;

    private Color baseColor;

    private void Awake()
    {
        if (targetText == null)
            targetText = GetComponent<TMP_Text>();

        if (targetText != null)
            baseColor = targetText.color;
    }

    private void Update()
    {
        if (targetText == null) return;

        float t = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = baseColor;
        c.a = alpha;
        targetText.color = c;
    }
}