using UnityEngine;

public class TreasurePickup : MonoBehaviour
{
    [SerializeField] private float addSeconds = 10f;

    [Header("Popup")]
    [SerializeField] private GameObject timeBonusPopupPrefab;
    [SerializeField] private Vector3 popupOffset = new Vector3(0f, 0.85f, 0f);

    private bool picked;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked) return;

        PlayerGridMover player = other.GetComponentInParent<PlayerGridMover>();
        if (player == null) return;

        picked = true;

        TimeLimitManager.I?.AddTime(addSeconds);

        if (timeBonusPopupPrefab == null)
        {
            Debug.LogWarning("TreasurePickup: Time Bonus Popup Prefab が設定されていません。");
            gameObject.SetActive(false);
            return;
        }

        Vector3 popupPos = transform.position + popupOffset;

        GameObject popup = Instantiate(
            timeBonusPopupPrefab,
            popupPos,
            Quaternion.identity
        );

        Debug.Log("TIME+10 Popup created: " + popup.name + " / pos: " + popupPos);

        gameObject.SetActive(false);
    }
}