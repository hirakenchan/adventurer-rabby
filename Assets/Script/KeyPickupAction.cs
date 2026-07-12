using UnityEngine;

public class KeyPickupAction : MonoBehaviour
{
    private bool picked;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (picked) return;

        // rabbi のタグを "Player" にしておくのがおすすめ
        if (!other.CompareTag("Player")) return;

        picked = true;
        BoardManager.I.ObtainKey();

        gameObject.SetActive(false); // 鍵だけ即消す
    }
}
