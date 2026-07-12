using System.Collections;
using UnityEngine;
using TMPro;

public class StageStartDrop : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private PlayerGridMover player;
    [SerializeField] private Transform visualTarget; // rabbi の Visual を入れる

    [Header("Drop Animation")]
    [SerializeField] private bool playOnFirstStage = false;
    [SerializeField] private float startOffsetY = 80f;
    [SerializeField] private float dropDelay = 0.2f;
    [SerializeField] private float dropDuration = 2.0f;

    [Header("Magic Hover")]
    [SerializeField] private float hoverHeight = 0.4f;
    [SerializeField] private float hoverDuration = 1.0f;
    [SerializeField] private float hoverBobAmount = 0.08f;
    [SerializeField] private float landingDuration = 0.45f;

    [Header("Ready Go UI")]
    [SerializeField] private TMP_Text readyGoText;
    [SerializeField] private bool showReadyGo = true;
    [SerializeField] private float readyDuration = 0.6f;
    [SerializeField] private float goDuration = 0.5f;

    private IEnumerator Start()
    {
        int currentFloor = FloorManager.I != null ? FloorManager.I.GetCurrentFloor() : 1;
        bool shouldPlayDrop = playOnFirstStage || currentFloor > 1;

        // ステージ開始時はいったんゲームを止める
        TimeLimitManager.I?.PauseTimer();

        if (player != null)
            player.SetControlEnabled(false);

        HideReadyGo();

        // READY表示
        if (showReadyGo)
        {
            ShowReadyGo("READY");
        }

        // STAGE 2以降：READYを表示したままrabbiが降りてくる
        if (shouldPlayDrop)
        {
            yield return StartCoroutine(DropRoutine());
        }
        else
        {
            // STAGE 1など、降下しない場合だけREADYを一定時間表示
            if (showReadyGo)
                yield return new WaitForSecondsRealtime(readyDuration);
        }

        // 着地後、少しだけ間を置く
        yield return new WaitForSecondsRealtime(0.15f);

        // GO表示
        if (showReadyGo)
        {
            ShowReadyGo("GO");
            yield return new WaitForSecondsRealtime(goDuration);
            HideReadyGo();
        }

        // ステージ開始時はいったんゲームを止める
        TimeLimitManager.I?.PauseTimer();
        GamePauseManager.I?.SetMenuEnabled(false);

        if (player != null)
            player.SetControlEnabled(false);

        // GOの後にゲーム開始
        TimeLimitManager.I?.ResumeTimer();

        if (player != null)
        {
            player.SetControlEnabled(true);
        }

        GamePauseManager.I?.SetMenuEnabled(true);
    }

    private IEnumerator DropRoutine()
    {
        Transform target = visualTarget != null
            ? visualTarget
            : player != null ? player.transform : null;

        if (target == null)
            yield break;

        Vector3 endLocalPos = target.localPosition;
        Vector3 startLocalPos = endLocalPos + Vector3.up * startOffsetY;
        Vector3 hoverLocalPos = endLocalPos + Vector3.up * hoverHeight;

        target.localPosition = startLocalPos;

        if (dropDelay > 0f)
            yield return new WaitForSecondsRealtime(dropDelay);

        // 1. 上からゆっくり降りてくる
        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / dropDuration);

            float ease = 1f - Mathf.Pow(1f - t, 3f);

            target.localPosition = Vector3.Lerp(startLocalPos, hoverLocalPos, ease);

            yield return null;
        }

        target.localPosition = hoverLocalPos;

        // 2. 着地前に1回だけふわっと浮く
        timer = 0f;

        while (timer < hoverDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / hoverDuration);

            float bob = Mathf.Sin(t * Mathf.PI) * hoverBobAmount;

            target.localPosition = hoverLocalPos + Vector3.up * bob;

            yield return null;
        }

        target.localPosition = hoverLocalPos;

        // 3. 最後にすっと着地
        Vector3 landingStartPos = target.localPosition;
        timer = 0f;

        while (timer < landingDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / landingDuration);

            float ease = t * t * (3f - 2f * t);

            target.localPosition = Vector3.Lerp(landingStartPos, endLocalPos, ease);

            yield return null;
        }

        target.localPosition = endLocalPos;
    }

    private void ShowReadyGo(string message)
    {
        if (readyGoText == null)
            return;

        readyGoText.gameObject.SetActive(true);
        readyGoText.text = message;
    }

    private void HideReadyGo()
    {
        if (readyGoText == null)
            return;

        readyGoText.text = "";
        readyGoText.gameObject.SetActive(false);
    }
}