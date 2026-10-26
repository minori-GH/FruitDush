using UnityEngine;

/// <summary>
/// 2D横スクロール向けのシンプルな敵巡回スクリプト
/// ・初期位置を中心に左右へ往復移動
/// ・端まで行ったら方向転換
/// ・移動方向に合わせてSpriteの向きを反転
/// </summary>
public class EnemyMove : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float speed = 2f;            // 移動速度
    [SerializeField] private float patrolDistance = 3f;   // 初期位置から左右に動く距離

    // 初期位置（巡回の中心）
    private Vector3 startPosition;

    // 移動方向: 1 = 右, -1 = 左
    private int moveDirection = 1;

    private void Start()
    {
        // シーン開始時の位置を保存
        startPosition = transform.position;
    }

    private void Update()
    {
        // 毎フレーム、左右どちらかへ移動
        // Time.deltaTime を掛けることでフレームレートに依存しない移動になる
        Vector3 nextPosition = transform.position;
        nextPosition.x += speed * moveDirection * Time.deltaTime;
        transform.position = nextPosition;

        // 初期位置からどれだけ離れたかを確認
        float offsetX = transform.position.x - startPosition.x;

        // 右端に到達したら左へ向きを変える
        if (offsetX >= patrolDistance)
        {
            moveDirection = -1;
            UpdateSpriteFlip();
        }
        // 左端に到達したら右へ向きを変える
        else if (offsetX <= -patrolDistance)
        {
            moveDirection = 1;
            UpdateSpriteFlip();
        }
    }

    /// <summary>
    /// 移動方向に合わせて見た目の向きを反転する
    /// </summary>
    private void UpdateSpriteFlip()
    {
        // localScale.x の絶対値を使って、向きだけ切り替える
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDirection;
        transform.localScale = scale;
    }

    private void OnDrawGizmosSelected()
    {
        // 巡回範囲をSceneビューで見やすく表示
        Gizmos.color = Color.cyan;

        // Play中は保存済みのstartPosition、Edit中は現在位置を中心に表示
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Vector3 left = new Vector3(center.x - patrolDistance, center.y, center.z);
        Vector3 right = new Vector3(center.x + patrolDistance, center.y, center.z);

        Gizmos.DrawLine(left, right);
        Gizmos.DrawWireSphere(left, 0.1f);
        Gizmos.DrawWireSphere(right, 0.1f);
    }
}
