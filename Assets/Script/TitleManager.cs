using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class RankingRowUI
{
    public TMP_Text stageText;
    public TMP_Text stepsText;
}

public class TitleManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("UI")]
    [SerializeField] private GameObject mainMenuGroup;
    [SerializeField] private GameObject rankingPanel;

    [Header("Ranking Rows")]
    [SerializeField] private RankingRowUI[] rankingRows = new RankingRowUI[10];

    [Header("Credits")]
    [SerializeField] private GameObject creditsPanel;

    private bool started;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);

        if (rankingPanel != null)
            rankingPanel.SetActive(false);
    }

    private void Update()
    {
        if (started) return;

        // PC確認用
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
    }

    public void StartGame()
    {
        if (started) return;

        started = true;
        Time.timeScale = 1f;

        SceneManager.LoadScene(gameSceneName);

        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    public void OpenRanking()
    {
        if (started) return;

        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(false);

        if (rankingPanel != null)
            rankingPanel.SetActive(true);

        UpdateRankingRows();
    }

    public void CloseRanking()
    {
        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);

        if (rankingPanel != null)
            rankingPanel.SetActive(false);
    }

    private void UpdateRankingRows()
    {
        List<RankingRecord> records = RankingManager.GetRecords();

        for (int i = 0; i < rankingRows.Length; i++)
        {
            RankingRowUI row = rankingRows[i];

            if (row == null) continue;

            if (i < records.Count)
            {
                RankingRecord record = records[i];

                if (row.stageText != null)
                    row.stageText.text = record.stage.ToString();

                if (row.stepsText != null)
                    row.stepsText.text = record.steps.ToString();
            }
            else
            {
                if (row.stageText != null)
                    row.stageText.text = "-";

                if (row.stepsText != null)
                    row.stepsText.text = "-";
            }
        }
    }

    public void OpenCredits()
    {
        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(false);

        if (creditsPanel != null)
            creditsPanel.SetActive(true);
    }

    public void CloseCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (mainMenuGroup != null)
            mainMenuGroup.SetActive(true);
    }

    public void OpenPrivacyPolicy()
    {
        Application.OpenURL("https://hirakenchan.github.io/adventurer-rabby/privacy-policy.html");
    }
}