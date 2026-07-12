using UnityEngine;

public class FloatBob : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.08f; // 揺れ幅（Unity座標）
    [SerializeField] private float speed = 2.0f;      // 速さ

    private Vector3 startLocalPos;

    private void Awake()
    {
        startLocalPos = transform.localPosition;
    }

    private void Update()
    {
        float y = Mathf.Sin(Time.time * speed) * amplitude;
        transform.localPosition = startLocalPos + new Vector3(0f, y, 0f);
    }
}