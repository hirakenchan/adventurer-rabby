using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameClearManager : MonoBehaviour
{
    public static GameClearManager I { get; private set; }

    [SerializeField] private GameObject clearUI;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private PlayerGridMover player;

    [Header("Continue With Ad")]
    [SerializeField] private Button continueButton;
    [SerializeField] private int maxContinueCount = 1;
    [SerializeField] private TMP_Text continueButtonText;
    [SerializeField] private Color continueReadyColor = Color.yellow;
    [SerializeField] private TMP_Text gameOverText;

    private Color continueDefaultColor;
    private string continueDefaultText;
    private bool continueTextCached = false;
    private bool continueRewardReady = false;

    private static int continueCount = 0;
    private bool isContinueProcessing = false;

    [Header("Next Prompt UI")]
    [SerializeField] private TMP_Text tapToNextText;
    [SerializeField] private float tapToNextDelay = 1.0f;
    [SerializeField] private float fadeSpeed = 2.0f;
    [SerializeField] private float minAlpha = 0.25f;
    [SerializeField] private float maxAlpha = 1.0f;

    private Coroutine tapToNextCoroutine;

    [Header("Game Over UI")]
    [SerializeField] private Image gameOverOverlay;
    [SerializeField] private GameObject gameOverButtonArea;
    [SerializeField] private GameObject newRecordArea;
    [SerializeField] private TMP_Text rankText;

    [Header("Game Over Effect")]
    [SerializeField] private float gameOverFadeDuration = 1.2f;
    [SerializeField] private float gameOverOverlayAlpha = 0.75f;
    [SerializeField] private float gameOverButtonDelayAfterFade = 0.5f;

    [Header("Game Over Confirm")]
    [SerializeField] private GameObject gameOverConfirmPanel;
    [SerializeField] private TMP_Text gameOverConfirmText;

    private enum GameOverConfirmAction
    {
        None,
        Retry,
        Title
    }

    private GameOverConfirmAction pendingConfirmAction = GameOverConfirmAction.None;

    [Header("Fly Up Effect")]
    [SerializeField] private Transform flyTarget;
    [SerializeField] private float flyDelay = 2f;
    [SerializeField] private float flyDuration = 2f;
    [SerializeField] private float flyDistance = 3f;

    [Header("Rabbi Particles")]
    [SerializeField] private ParticleSystem rabbiTrailParticles;
    [SerializeField] private float particleStopAfter = 2f;

    [Header("Proceed Input")]
    [SerializeField] private bool enableProceedInput = true;
    [SerializeField] private float clearProceedDelayAfterFly = 0.2f;

    private bool cleared;
    private bool gameOvered;
    private bool canProceed;

    private Coroutine particleStopCoroutine;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        HideTapToNextText();

        if (clearUI != null)
            clearUI.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (newRecordArea != null)
            newRecordArea.SetActive(false);

        SetGameOverOverlayAlpha(0f);

        canProceed = false;

        if (gameOverConfirmPanel != null)
            gameOverConfirmPanel.SetActive(false);
        }

    private void Update()
    {
        if (!enableProceedInput) return;

        // ゲームオーバー時は、クリック・タップでリスタートしない
        // RETRY / TITLE ボタンで操作する
        if (!cleared) return;
        if (!canProceed) return;

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            RestartStage();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            RestartStage();
            return;
        }

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            RestartStage();
            return;
        }
    }

    public void Clear()
    {
        if (cleared || gameOvered)
            return;

        cleared = true;
        canProceed = false;

        // ここで即操作停止
        if (player != null)
            player.SetControlEnabled(false);

        TimeLimitManager.I?.StopTimer();
        GamePauseManager.I?.SetMenuEnabled(false);

        if (clearUI != null)
            clearUI.SetActive(true);

        StartCoroutine(FlyUpRoutine());
    }

    public void GameOver()
    {
        Debug.Log("GameOver() called");

        if (cleared || gameOvered)
        {
            Debug.Log($"GameOver skipped. cleared={cleared}, gameOvered={gameOvered}");
            return;
        }

        gameOvered = true;
        canProceed = false;

        int floor = FloorManager.I != null ? FloorManager.I.GetCurrentFloor() : 1;
        int steps = StepManager.I != null ? StepManager.I.GetSteps() : 0;

        Debug.Log($"GameOver result. floor={floor}, steps={steps}");

        int rank = RankingManager.SaveResult(floor, steps);
        bool isNewRecord = rank > 0;

        if (isNewRecord)
        {
            Debug.Log($"New Record! Rank {rank}");
        }
        else
        {
            Debug.Log("Not ranked.");
        }

        TimeLimitManager.I?.StopTimer();
        GamePauseManager.I?.SetMenuEnabled(false);

        if (player != null)
            player.SetControlEnabled(false);

        StartCoroutine(GameOverRoutine(isNewRecord, rank));
    }

    private IEnumerator GameOverRoutine(bool isNewRecord, int rank)
    {
        ResetContinueButtonText();

        if (gameOverConfirmPanel != null)
            gameOverConfirmPanel.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (newRecordArea != null)
            newRecordArea.SetActive(false);

        SetGameOverOverlayAlpha(0f);

        yield return StartCoroutine(FadeGameOverOverlay(0f, gameOverOverlayAlpha, gameOverFadeDuration));

        if (isNewRecord)
        {
            if (newRecordArea != null)
                newRecordArea.SetActive(true);

            if (rankText != null)
                rankText.text = $"RANK {rank}";
        }

        if (gameOverButtonDelayAfterFade > 0f)
            yield return new WaitForSecondsRealtime(gameOverButtonDelayAfterFade);

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(true);
        
        if (continueButton != null)
        {
            bool canContinue = continueCount < maxContinueCount;
            continueButton.gameObject.SetActive(canContinue);
            continueButton.interactable = canContinue;
        }
    }

    private IEnumerator FadeGameOverOverlay(float from, float to, float duration)
    {
        if (gameOverOverlay == null) yield break;

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / duration);

            float alpha = Mathf.Lerp(from, to, t);
            SetGameOverOverlayAlpha(alpha);

            yield return null;
        }

        SetGameOverOverlayAlpha(to);
    }

    private void SetGameOverOverlayAlpha(float alpha)
    {
        if (gameOverOverlay == null) return;

        Color color = gameOverOverlay.color;
        color.a = alpha;
        gameOverOverlay.color = color;
    }

    public void OnClickRetry()
    {
        if (continueRewardReady)
        {
            OpenGameOverConfirm(GameOverConfirmAction.Retry);
            return;
        }

        ExecuteRetry();
    }

    public void OnClickTitle()
    {
        if (continueRewardReady)
        {
            OpenGameOverConfirm(GameOverConfirmAction.Title);
            return;
        }

        ExecuteTitle();
    }

    private IEnumerator FlyUpRoutine()
    {
        yield return new WaitForSeconds(flyDelay);

        if (player == null) yield break;

        Transform t = (flyTarget != null) ? flyTarget : player.transform;

        PlayRabbiParticles();

        Vector3 start = t.position;
        Vector3 end = start + Vector3.up * flyDistance;

        float e = 0f;
        while (e < flyDuration)
        {
            e += Time.deltaTime;

            float k = Mathf.Clamp01(e / flyDuration);
            k = k * k * (3f - 2f * k);

            t.position = Vector3.Lerp(start, end, k);

            yield return null;
        }

        t.position = end;

        if (clearProceedDelayAfterFly > 0f)
            yield return new WaitForSeconds(clearProceedDelayAfterFly);

        canProceed = true;

        if (tapToNextCoroutine != null) StopCoroutine(tapToNextCoroutine);

        tapToNextCoroutine = StartCoroutine(ShowTapToNextTextRoutine());
    }

    private void PlayRabbiParticles()
    {
        if (rabbiTrailParticles == null) return;

        rabbiTrailParticles.gameObject.SetActive(true);
        rabbiTrailParticles.Clear(true);

        var em = rabbiTrailParticles.emission;
        em.enabled = true;

        rabbiTrailParticles.Play(true);

        if (particleStopCoroutine != null)
            StopCoroutine(particleStopCoroutine);

        particleStopCoroutine = StartCoroutine(StopParticlesAfterSeconds(particleStopAfter));
    }

    private IEnumerator StopParticlesAfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (rabbiTrailParticles != null)
        {
            rabbiTrailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    private void RestartStage()
    {
        if (cleared)
        {
            FloorManager.I?.GoNextFloor();
        }

        HideTapToNextText();

        Time.timeScale = 1f;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void HideTapToNextText()
    {
        if (tapToNextCoroutine != null)
        {
            StopCoroutine(tapToNextCoroutine);
            tapToNextCoroutine = null;
        }

        if (tapToNextText != null)
        {
            var c = tapToNextText.color;
            c.a = 1f;
            tapToNextText.color = c;

            tapToNextText.gameObject.SetActive(false);
            tapToNextText.text = "";
        }
    }

    private IEnumerator ShowTapToNextTextRoutine()
    {
        yield return new WaitForSecondsRealtime(tapToNextDelay);

        if (!cleared || !canProceed)
            yield break;

        if (tapToNextText == null)
            yield break;

        tapToNextText.text = "TAP TO NEXT";
        tapToNextText.gameObject.SetActive(true);

        // 最初は完全に透明
        Color c = tapToNextText.color;
        c.a = 0f;
        tapToNextText.color = c;

        // まずはゆっくりフェードイン
        float fadeInDuration = 0.8f;
        float timer = 0f;

        while (timer < fadeInDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / fadeInDuration);

            c.a = Mathf.Lerp(0f, maxAlpha, t);
            tapToNextText.color = c;

            yield return null;
        }

        // その後、ふんわりフェードを繰り返す
        float waveTime = Mathf.PI / 2f; 
        // maxAlpha から自然に始めるための初期値

        while (cleared && canProceed)
        {
            waveTime += Time.unscaledDeltaTime * fadeSpeed;

            float wave = (Mathf.Sin(waveTime) + 1f) * 0.5f; // 0〜1
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);

            c.a = alpha;
            tapToNextText.color = c;

            yield return null;
        }
    }
    public void OnClickContinueWithAd()
    {
        if (continueRewardReady)
        {
            ContinueCurrentStage();
            return;
        }

        if (isContinueProcessing)
            return;

        if (isContinueProcessing)
            return;

        if (continueCount >= maxContinueCount)
        {
            Debug.Log("Continue limit reached.");
            return;
        }

        if (RewardedAdManager.I == null)
        {
            Debug.LogWarning("RewardedAdManager is missing.");
            return;
        }

        isContinueProcessing = true;

        RewardedAdManager.I.ShowRewardedAd(
            onRewarded: ShowContinueReady,
            onFailed: () =>
            {
                isContinueProcessing = false;

                if (continueButton != null)
                    continueButton.interactable = true;
            }
        );
    }

    private void ContinueCurrentStage()
    {
        continueRewardReady = false;

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(false);

        if (newRecordArea != null)
            newRecordArea.SetActive(false);

        if (gameOverText != null)
            gameOverText.gameObject.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(true);

        SetGameOverOverlayAlpha(1f);

        continueCount++;

        Time.timeScale = 1f;

        // コンティニューではSTEPSをリセットしない
        // StepManager.I?.ResetSteps();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ShowContinueReady()
    {
        isContinueProcessing = false;
        continueRewardReady = true;

        if (continueButton != null)
        {
            continueButton.interactable = true;
            continueButton.gameObject.SetActive(true);
        }

        CacheContinueButtonText();

        if (continueButtonText != null)
        {
            continueButtonText.text = "[ TAP TO CONTINUE ]";
            continueButtonText.color = continueReadyColor;
        }
    }

    private void CacheContinueButtonText()
    {
        if (continueTextCached)
            return;

        if (continueButtonText == null && continueButton != null)
            continueButtonText = continueButton.GetComponentInChildren<TMP_Text>();

        if (continueButtonText == null)
            return;

        continueDefaultText = continueButtonText.text;
        continueDefaultColor = continueButtonText.color;
        continueTextCached = true;
    }

    private void ResetContinueButtonText()
    {
        CacheContinueButtonText();

        if (continueButtonText == null)
            return;

        continueButtonText.text = continueDefaultText;
        continueButtonText.color = continueDefaultColor;
    }

    private void OpenGameOverConfirm(GameOverConfirmAction action)
    {
        pendingConfirmAction = action;

        if (gameOverConfirmText != null)
        {
            if (action == GameOverConfirmAction.Retry)
                gameOverConfirmText.text = "RETRY ?";
            else if (action == GameOverConfirmAction.Title)
                gameOverConfirmText.text = "GO TITLE ?";
        }

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(false);

        if (gameOverConfirmPanel != null)
            gameOverConfirmPanel.SetActive(true);
    }

    public void OnClickGameOverConfirmYes()
{
    if (gameOverConfirmPanel != null)
        gameOverConfirmPanel.SetActive(false);

    if (pendingConfirmAction == GameOverConfirmAction.Retry)
    {
        ExecuteRetry();
    }
    else if (pendingConfirmAction == GameOverConfirmAction.Title)
    {
        ExecuteTitle();
    }

    pendingConfirmAction = GameOverConfirmAction.None;
}

    public void OnClickGameOverConfirmNo()
    {
        pendingConfirmAction = GameOverConfirmAction.None;

        if (gameOverConfirmPanel != null)
            gameOverConfirmPanel.SetActive(false);

        if (gameOverButtonArea != null)
            gameOverButtonArea.SetActive(true);

        if (continueRewardReady && continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }
    }

    private void ExecuteRetry()
    {
        continueRewardReady = false;
        continueCount = 0;

        if (gameOvered)
        {
            FloorManager.I?.ResetFloor();
            StepManager.I?.ResetSteps();
        }

        Time.timeScale = 1f;

        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    private void ExecuteTitle()
    {
        continueRewardReady = false;
        continueCount = 0;

        FloorManager.I?.ResetFloor();
        StepManager.I?.ResetSteps();

        Time.timeScale = 1f;
        SceneManager.LoadScene("TitleScene");
    }
}