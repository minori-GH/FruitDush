using UnityEngine;

public class StartGoalTrigger : MonoBehaviour
{
    // 最初に外へ出たかどうか。スタート直後はfalse
    private bool hasLeft = false;

    // クリア通知先
    private GameManager gameManager;

    private void Awake()
    {
        // シーンからGameManagerを探して保存する。
        gameManager = FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogWarning("StartGoalTrigger: GameManager が見つかりません。");
        }
    }
    // Triggerから「OnTriggerExit出た瞬間」に呼ばれる
    private void OnTriggerExit2D(Collider2D other)
    {
        // Player以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // 最初に外へ出た瞬間だけ hasLeft を true にする
        if (!hasLeft)
        {
            hasLeft = true;
            Debug.Log("StartGoalTrigger: Playerがスタート地点を出ました。hasLeft = true");
        }
    }
    // Triggerに入ったときに呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        Debug.Log("StartGoalTrigger: PlayerがTriggerに入りました。hasLeft = " + hasLeft);

        // 一度外に出たあとで戻ってきたらクリア
        if (hasLeft)
        {
            if (gameManager != null)
            {
                gameManager.OnGameClear();
            }
            else
            {
                Debug.LogWarning("StartGoalTrigger: GameManager が null のためクリア通知できません。");
            }
        }
    }
}