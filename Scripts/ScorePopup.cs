using UnityEngine;
using TMPro;

public class ScorePopup : MonoBehaviour
{
    // TextMeshPro参照
    [SerializeField] private TMP_Text scoreText;

    // 上に動く速さ
    [SerializeField] private float moveSpeed = 1.5f;
    // 表示時間（秒）
    [SerializeField] private float lifeTime = 1f;
    private float timer = 0f;

    // 外部からスコア表示を設定する
    public void Setup(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "+" + score.ToString();
        }
    }

    private void Update()
    {
        // 上方向に移動
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 一定時間経過で削除
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}