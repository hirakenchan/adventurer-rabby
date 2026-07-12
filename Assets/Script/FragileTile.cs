using System.Collections;
using UnityEngine;

public class FragileTile : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Collider2D col;

    [Header("Arrive (踏んだ感)")]
    [SerializeField] private float arriveSquishDuration = 0;
    [SerializeField] private float arriveSquishAmount = 0;

    [Header("Fall (落下演出)")]
    [SerializeField] private float shakeDuration = 0.60f;   // ← 少し長め（ゆっくり感）
    [SerializeField] private float shakeAmount = 0.03f;

    [SerializeField] private float fallDuration = 1.2f;     // ← ここを上げると“ゆっくり崩れ落ちる”
    [SerializeField] private float fallDistance = 1.0f;     // ← 下に沈む量
    [SerializeField] private float shrinkTo = 0.18f;        // ← 最後の縮小率（小さいほど消える）

    [SerializeField] private bool hideChildrenOnCollapse = true;

    [Header("Particles (optional)")]
    [SerializeField] private ParticleSystem crumbleParticles;

    public bool IsCollapsed { get; private set; }
    public bool IsCollapsing { get; private set; } // ★追加：崩れ中

    public Vector2Int Cell { get; private set; }

    private Vector3 baseLocalPos;
    private Vector3 baseLocalScale;

    private Coroutine arriveCo;
    private Coroutine collapseCo;

    private void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (col == null) col = GetComponent<Collider2D>();

        baseLocalPos = transform.localPosition;
        baseLocalScale = transform.localScale;

        if (crumbleParticles == null)
        {
            var t = transform.Find("CrumbleParticles");
            if (t != null) crumbleParticles = t.GetComponent<ParticleSystem>();
        }

        if (crumbleParticles != null)
            crumbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void SetCell(Vector2Int cell) => Cell = cell;

    public void OnPlayerArrived()
    {
        if (IsCollapsed || IsCollapsing) return;

        if (arriveCo != null) StopCoroutine(arriveCo);
        arriveCo = StartCoroutine(ArriveSquish());
    }

    public void Collapse()
    {
        if (IsCollapsed || IsCollapsing) return;

        // ★崩れ始めた瞬間に「入れない」扱いにする
        IsCollapsing = true;

        // 先に当たり判定は消す（物理用。Grid移動でも念のため）
        if (col != null) col.enabled = false;

        collapseCo = StartCoroutine(FallRoutine());
    }

    private IEnumerator ArriveSquish()
    {
        float t = 0f;
        Vector3 from = baseLocalScale;
        Vector3 to = new Vector3(
            baseLocalScale.x * (1f + arriveSquishAmount),
            baseLocalScale.y * (1f - arriveSquishAmount),
            baseLocalScale.z
        );

        while (t < arriveSquishDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / arriveSquishDuration);
            transform.localScale = Vector3.Lerp(from, to, k);
            yield return null;
        }

        t = 0f;
        while (t < arriveSquishDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / arriveSquishDuration);
            transform.localScale = Vector3.Lerp(to, from, k);
            yield return null;
        }

        transform.localScale = baseLocalScale;
        arriveCo = null;
    }

    private IEnumerator FallRoutine()
    {
        // ちょい揺れ
        float t = 0f;
        while (t < shakeDuration)
        {
            t += Time.deltaTime;
            Vector2 r = Random.insideUnitCircle * shakeAmount;
            transform.localPosition = baseLocalPos + new Vector3(r.x, r.y, 0f);
            yield return null;
        }

        // パーティクル（任意）
        if (crumbleParticles != null)
        {
            crumbleParticles.gameObject.SetActive(true);
            crumbleParticles.Play(true);
        }

        // 落下（沈む＋縮む）
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = baseLocalPos + Vector3.down * fallDistance;

        Vector3 startScale = baseLocalScale;
        Vector3 endScale = baseLocalScale * shrinkTo;

        // 透明度：ゆっくり消える
        Color startColor = (sr != null) ? sr.color : Color.white;

        float e = 0f;
        while (e < fallDuration)
        {
            e += Time.deltaTime;
            float k = Mathf.Clamp01(e / fallDuration);

            // じわっと落ちる（EaseIn）
            float ease = k * k;

            transform.localPosition = Vector3.Lerp(startPos, endPos, ease);
            transform.localScale = Vector3.Lerp(startScale, endScale, ease);

            if (sr != null)
            {
                var c = startColor;
                c.a = Mathf.Lerp(startColor.a, 0f, ease);
                sr.color = c;
            }

            yield return null;
        }

        // 完全に消す
        transform.localPosition = endPos;
        transform.localScale = endScale;

        if (sr != null) sr.enabled = false;

        if (hideChildrenOnCollapse)
        {
            foreach (Transform ch in transform)
                ch.gameObject.SetActive(false);
        }

        IsCollapsed = true;
        IsCollapsing = false;
        collapseCo = null;
    }
}
