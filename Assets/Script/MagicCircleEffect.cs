using System.Collections;
using UnityEngine;

public class MagicCircleEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ParticleSystem particles;
    [SerializeField] private Transform visual;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;

    [Header("Color Fade")]
    [SerializeField] private SpriteRenderer circleRenderer;
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private float fadeSeconds = 0.5f; // ←好みで0.3〜1.0くらい

    private bool active;
    private Coroutine fadeCo;

    private IEnumerator Start()
    {
        yield return null;

        if (visual == null) visual = transform;

        if (circleRenderer == null)
            circleRenderer = visual.GetComponentInChildren<SpriteRenderer>(true);

        if (particles == null)
            particles = GetComponentInChildren<ParticleSystem>(true);

        // 初期：グレー + パーティクル停止
        if (circleRenderer != null) circleRenderer.color = lockedColor;

        if (particles != null)
        {
            particles.gameObject.SetActive(true);
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Clear(true);
        }

        BoardManager.I.ConditionsChanged += OnConditionsChanged;
        OnConditionsChanged();
    }

    private void OnDestroy()
    {
        if (BoardManager.I != null)
            BoardManager.I.ConditionsChanged -= OnConditionsChanged;
    }

    private void OnConditionsChanged()
    {
        if (!BoardManager.I.IsUnlocked)
            return;

        // すでに演出中なら何もしない
        if (active) return;

        active = true;

        // ★色をフェード
        if (fadeCo != null) StopCoroutine(fadeCo);
        if (circleRenderer != null)
            fadeCo = StartCoroutine(FadeColor(circleRenderer, lockedColor, unlockedColor, fadeSeconds));

        // ★パーティクル開始（最初は少なめにしたいならここでPlay→徐々に増やすも可能）
        if (particles != null)
        {
            var em = particles.emission;
            em.enabled = true;
            particles.Play(true);
        }
    }

    private IEnumerator FadeColor(SpriteRenderer r, Color from, Color to, float sec)
    {
        float t = 0f;
        while (t < sec)
        {
            t += Time.deltaTime;
            float k = (sec <= 0f) ? 1f : Mathf.Clamp01(t / sec);
            r.color = Color.Lerp(from, to, k);
            yield return null;
        }
        r.color = to;
    }

    private void Update()
    {
        if (!active) return;
        visual.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
