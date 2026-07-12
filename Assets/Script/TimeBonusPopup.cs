using System.Collections;
using TMPro;
using UnityEngine;

public class TimeBonusPopup : MonoBehaviour
{
    [SerializeField] private TMP_Text text;

    [Header("Display")]
    [SerializeField] private float riseDuration = 0.35f; // 上に移動する時間
    [SerializeField] private float holdDuration = 0.25f; // 移動後にはっきり見せる時間
    [SerializeField] private float fadeDuration = 0.7f;  // その場で消える時間
    [SerializeField] private float upDistance = 0.45f;   // 上に移動する距離

    [Header("Sorting")]
    [SerializeField] private string sortingLayerName = "Player";
    [SerializeField] private int sortingOrder = 9999;

    private void Awake()
    {
        if (text == null)
            text = GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            Debug.LogWarning("TimeBonusPopup: TMP_Text が見つかりません。Prefabを確認してください。");
            Destroy(gameObject);
            return;
        }

        text.text = "TIME+5";

        Color c = text.color;
        c.a = 1f;
        text.color = c;

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            r.sortingLayerName = sortingLayerName;
            r.sortingOrder = sortingOrder;
        }
    }

    private void Start()
    {
        StartCoroutine(Play());
    }

    private IEnumerator Play()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * upDistance;

        Color startColor = text.color;
        startColor.a = 1f;
        text.color = startColor;

        // 1. はっきり表示したまま上に移動
        float t = 0f;

        while (t < riseDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / riseDuration);

            // 少しなめらかに上がる
            float ease = k * k * (3f - 2f * k);

            transform.position = Vector3.Lerp(startPos, endPos, ease);

            // 移動中は透明にしない
            text.color = startColor;

            yield return null;
        }

        transform.position = endPos;

        // 2. 移動後、少しだけそのまま表示
        yield return new WaitForSeconds(holdDuration);

        // 3. その場でフェードアウト
        t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);

            Color c = startColor;
            c.a = Mathf.Lerp(1f, 0f, k);
            text.color = c;

            // ここでは移動しない
            transform.position = endPos;

            yield return null;
        }

        Destroy(gameObject);
    }
}