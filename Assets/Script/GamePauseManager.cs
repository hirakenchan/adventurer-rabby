using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePauseManager : MonoBehaviour
{
    public static GamePauseManager I { get; private set; }

    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject pauseMenuGroup;
    [SerializeField] private GameObject titleConfirmPanel;

    [SerializeField] private PlayerGridMover player;

    [Header("Menu Button")]
    [SerializeField] private Button menuButton;

    [Header("Title Scene")]
    [SerializeField] private string titleSceneName = "TitleScene";

    private bool isPaused;
    private bool menuEnabled = false;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseMenuGroup != null)
            pauseMenuGroup.SetActive(true);

        if (titleConfirmPanel != null)
            titleConfirmPanel.SetActive(false);

        Time.timeScale = 1f;
        SetMenuEnabled(false);
    }

    public void OpenMenu()
    {
        if (!menuEnabled) return;
        if (isPaused) return;

        isPaused = true;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (pauseMenuGroup != null)
            pauseMenuGroup.SetActive(true);

        if (titleConfirmPanel != null)
            titleConfirmPanel.SetActive(false);

        if (player != null)
            player.SetControlEnabled(false);

        Time.timeScale = 0f;
    }

    public void ContinueGame()
    {
        if (!isPaused) return;

        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (titleConfirmPanel != null)
            titleConfirmPanel.SetActive(false);

        if (player != null)
        {
            player.SetControlEnabled(true);
        }
        
        SetMenuEnabled(true);

        Time.timeScale = 1f;
    }

    public void OpenTitleConfirm()
    {
        if (!isPaused) return;

        if (pauseMenuGroup != null)
            pauseMenuGroup.SetActive(false);

        if (titleConfirmPanel != null)
            titleConfirmPanel.SetActive(true);
    }

    public void CancelTitleConfirm()
    {
        if (!isPaused) return;

        if (titleConfirmPanel != null)
            titleConfirmPanel.SetActive(false);

        if (pauseMenuGroup != null)
            pauseMenuGroup.SetActive(true);
    }

    public void BackToTitle()
    {
        Time.timeScale = 1f;

        FloorManager.I?.ResetFloor();
        StepManager.I?.ResetSteps();

        SceneManager.LoadScene(titleSceneName);
    }

    public void SetMenuEnabled(bool enabled)
    {
        menuEnabled = enabled;

        if (menuButton != null)
            menuButton.interactable = enabled;
    }
}