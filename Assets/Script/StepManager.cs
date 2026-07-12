using TMPro;
using UnityEngine;

public class StepManager : MonoBehaviour
{
    public static StepManager I { get; private set; }

    [SerializeField] private TMP_Text stepsValueText;

    // アプリ起動中だけ保持
    private static int steps = 0;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        UpdateStepsText();
    }

    public void AddStep()
    {
        steps++;
        UpdateStepsText();
    }

    public int GetSteps()
    {
        return steps;
    }

    public void ResetSteps()
    {
        steps = 0;
        UpdateStepsText();
    }

    private void UpdateStepsText()
    {
        if (stepsValueText == null)
        {
            Debug.LogWarning("StepManager: StepsValueText が設定されていません。");
            return;
        }

        stepsValueText.text = steps.ToString();
    }
}