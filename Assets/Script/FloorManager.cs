using TMPro;
using UnityEngine;

public class FloorManager : MonoBehaviour
{
    public static FloorManager I { get; private set; }

    [SerializeField] private TMP_Text floorValueText;

    // アプリ起動中だけ保持
    private static int currentFloor = 1;

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        UpdateFloorText();
    }

    private void UpdateFloorText()
    {
        if (floorValueText == null)
        {
            Debug.LogWarning("FloorManager: FloorValueText が設定されていません。");
            return;
        }

        floorValueText.text = currentFloor.ToString();
    }

    public int GetCurrentFloor()
    {
        return currentFloor;
    }

    public void GoNextFloor()
    {
        currentFloor++;
        UpdateFloorText();
    }

    public void ResetFloor()
    {
        currentFloor = 1;
        UpdateFloorText();
    }
}