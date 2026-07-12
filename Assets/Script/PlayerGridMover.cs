using System.Collections;
using UnityEngine;

public class PlayerGridMover : MonoBehaviour
{
    [SerializeField] private float moveSeconds = 0.12f;
    [SerializeField] private float zOffset = -3f; // ★手前に出す（カメラが-10なら -1 は手前）

    private Vector2Int cell;
    private bool moving;
    private bool canControl = true;
    private void Start()
    {
        cell = BoardManager.I.startCell;
        var p = BoardManager.I.GetWorldPosition(cell);
        p.z = zOffset;                 // ★ここが大事
        transform.position = p;
    }

    private void Update()
    {
        if (!canControl) return;
        if (moving) return;

        if (Input.GetKeyDown(KeyCode.UpArrow))    TryMove(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.DownArrow))  TryMove(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.LeftArrow))  TryMove(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.RightArrow)) TryMove(Vector2Int.right);
    }

    public void MoveUpButton()
    {
        if (!canControl) return;
        TryMove(Vector2Int.up);
    }

    public void MoveDownButton()
    {
        if (!canControl) return;
        TryMove(Vector2Int.down);
    }

    public void MoveLeftButton()
    {
        if (!canControl) return;
        TryMove(Vector2Int.left);
    }

    public void MoveRightButton()
    {
        if (!canControl) return;
        TryMove(Vector2Int.right);
    }

    private void TryMove(Vector2Int dir)
    {
        if (!canControl) return;
        
        var next = cell + dir;
        if (!BoardManager.I.CanEnter(next)) return;

        var from = cell;
        cell = next;

        StartCoroutine(MoveRoutine(from, cell));
    }

    private IEnumerator MoveRoutine(Vector2Int fromCell, Vector2Int toCell)
    {
        moving = true;

        Vector3 fromW = BoardManager.I.GetWorldPosition(fromCell);
        Vector3 toW   = BoardManager.I.GetWorldPosition(toCell);
        fromW.z = zOffset; // ★移動中もz固定
        toW.z   = zOffset;

        float t = 0f;
        while (t < moveSeconds)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(fromW, toW, t / moveSeconds);
            yield return null;
        }

        transform.position = toW;
        BoardManager.I.OnPlayerMoved(fromCell, toCell);

        moving = false;
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
    }
}