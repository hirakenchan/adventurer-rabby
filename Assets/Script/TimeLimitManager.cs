using TMPro;
using UnityEngine;

public class TimeLimitManager : MonoBehaviour
{
    public static TimeLimitManager I { get; private set; }

    [SerializeField] private TMP_Text timeText;

    [Header("Time Limit")]
    [SerializeField] private float timeLimit = 60f;

    [Header("Warning")]
    [SerializeField] private int warningSeconds = 10;
    [SerializeField] private string warningColorCode = "#FF3333";

    private float remainingTime;
    private bool isRunning = true;
    private bool isPaused = false;
    private bool isStopped = false;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        remainingTime = timeLimit;
        UpdateTimeText();
    }

    private void Update()
    {
        if (isPaused || isStopped) return;

        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimeText();

            isRunning = false;
            GameClearManager.I?.GameOver();
            return;
        }

        UpdateTimeText();
    }

    private void UpdateTimeText()
    {
        if (timeText == null) return;

        int displayTime = Mathf.CeilToInt(remainingTime);

        if (displayTime <= warningSeconds)
        {
            timeText.text = $"TIME <color={warningColorCode}>{displayTime}</color>";
        }
        else
        {
            timeText.text = $"TIME {displayTime}";
        }
    }

    public void PauseTimer()
    {
        isPaused = true;
    }

    public void ResumeTimer()
    {
        if (isStopped)
            return;

        isPaused = false;
    }

    public void StopTimer()
    {
        isStopped = true;
        isPaused = true;
    }

    public void AddTime(float seconds)
    {
        if (!isRunning) return;

        remainingTime += seconds;
        UpdateTimeText();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void ResetTimer()
    {
        remainingTime = timeLimit;
        isRunning = true;
        UpdateTimeText();
    }
}