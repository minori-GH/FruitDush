using UnityEngine;

public class FruitItem : MonoBehaviour
{
    [Header("Banana=5 / Apple=10 / Watermelon=15")]
    public int scoreValue = 5;

    // ScorePopupのPrefab参照
    [Header("Popup")]
    [SerializeField] private ScorePopup scorePopupPrefab;

    private GameManager gameManager;

    private void Awake()
    {
        // シーン内のGameManagerを探してキャッシュ
        gameManager = FindFirstObjectByType<GameManager>();

        // GameManagerが見つからなかった場合コンソールに表示
        if (gameManager == null)
        {
            Debug.LogWarning("GameManager が見つかりません。スコア加算できません。");
        }
    }

    // TriggerがONのColliderで発動し、「何かが触れた瞬間」に呼ばれる
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Playerタグ以外は無視
        if (!other.CompareTag("Player"))
        {
            return;
        }

        // GameManagerがあればスコア加算
        if (gameManager != null)
        {
            gameManager.AddScore(scoreValue);
        }
        else
        {
            Debug.LogWarning("GameManager が null のためスコア加算をスキップしました。");
        }

        // アイテム取得SE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayItemGetSE();
        }

        // ポップアップPrefabを生成してスコア表示
        if (scorePopupPrefab != null)
        {
            ScorePopup popup = Instantiate(scorePopupPrefab, transform.position, Quaternion.identity);
            popup.Setup(scoreValue);
        }

        // 取得後にフルーツを消す
        Destroy(gameObject);
    }
}