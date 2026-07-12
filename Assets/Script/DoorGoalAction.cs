using UnityEngine;

public class DoorGoalAction : TileAction
{
    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openVisual;

    private void OnEnable()
    {
        if (BoardManager.I != null)
            BoardManager.I.ConditionsChanged += Refresh;
    }

    private void OnDisable()
    {
        if (BoardManager.I != null)
            BoardManager.I.ConditionsChanged -= Refresh;
    }

    private void Start() => Refresh();

    private void Refresh()
    {
        bool open = BoardManager.I.IsUnlocked;
        if (closedVisual) closedVisual.SetActive(!open);
        if (openVisual) openVisual.SetActive(open);
    }

    public override void OnPlayerStepped()
    {
        if (BoardManager.I.IsUnlocked)
        {
            Debug.Log("ステージクリア！🎉");
        }
        else
        {
            Debug.Log("扉が開かない…鍵やスイッチがまだ足りないみたいです。");
        }
    }
}
