using UnityEngine;

public class EnemyContact : MonoBehaviour
{
    // Triggerに何かが入った瞬間に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Playerタグ以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // PlayerControllerを取得（なければ何もしない）
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController == null)
        {
            return;
        }

        // 敵のワールド座標を渡して通知
        Vector2 enemyWorldPosition = transform.position;
        playerController.OnEnemyHit(enemyWorldPosition);
    }
}
