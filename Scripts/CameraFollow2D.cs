using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    // カメラが追いかける対象
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    // 進行方向の前方オフセット　プレイヤーのちょっと前を見る
    [SerializeField] private float forwardOffset = 2f;
    // カメラの追従スピード（大きいほどピタッと追従する）
    [SerializeField] private float smoothSpeed = 8f;


    // PlayerControllerを参照
    private PlayerController playerController;

    // カメラのY軸を固定
    private float fixedY;

    // 向きを数字で管理する変数 1=右, -1=左 trueやfalseだと計算に使いにくいため　速度が0のときも前向きを維持
    private int facingSign = 1;

    // カメラのZ軸を固定
    private const float FixedZ = -10f;

    private void Awake()
    {
        // カメラ開始時のYを固定値として保存
        fixedY = transform.position.y;
    }

    // ゲーム開始時に１回だけ実行
    private void Start()
    {
        // targetが設定されていればPlayerControllerを取得
        CachePlayerController();
    }

    // 毎フレーム読み込まれる　カメラは基本これ
    private void LateUpdate()
    {
        // nullチェック: target未設定なら処理しない
        if (target == null)
        {
            return;
        }

        // 参照が未取得なら再取得
        if (playerController == null)
        {
            CachePlayerController();
        }

        // PlayerControllerの進行方向を使う
        UpdateFacingFromPlayerController();

        // その向き（進行方向）に応じてカメラを動かす
        FollowCameraWithCurrentFacing();
    }

    // targetからPlayerControllerを取得
    private void CachePlayerController()
    {
        if (target == null)
        {
            playerController = null;
            return;
        }

        playerController = target.GetComponent<PlayerController>();
    }

    // カメラの向き（facingSign）をプレイヤーに合わせて更新する関数
    private void UpdateFacingFromPlayerController()
    {
        if (playerController == null)
        {
            return;
        }

        // スタン中は向きを更新しない
        if (playerController.IsStunned())
        {
            return; 
        }

        int dir = playerController.GetMoveDirection();
        facingSign = (dir >= 0) ? 1 : -1;
    }

    //現在の向き(facingSign)を使ってカメラを滑らかに追従
    private void FollowCameraWithCurrentFacing()
    {
        // playerController.IsStunned()（プレイヤーが「ノックバック中か？」を聞いている）
        bool isStunned = (playerController != null && playerController.IsStunned());

        // スタン時と通常時でX計算を分岐
        float targetX = target.position.x + (forwardOffset * facingSign);

        // Y固定、Z固定(-10)
        Vector3 desired = new Vector3(targetX, fixedY, FixedZ);

        // スタン中は即時追従
        if (isStunned)
        {
            transform.position = desired;
        }
        else
        {
            // フレームレートに依存せずに今の位置から目標位置に滑らかに近づける　
            // Time.deltaTime（1フレームにかかった時間　例）60FPS=約0.016秒）
            // Mathf.Exp()（最初は速く動いてだんだん減速してピタッと止まる）
            // Lerp（AからBに少しずつ移動する関数　XYZそれぞれに同時に適用される）
            // Vector3.Lerp(A, B, t) 　A=今の位置（transform.position）、B=目標位置（desired）、t=どれくらい近づくか（0〜1）
            // AからBへ、tの割合だけ進んだ位置を返す
            float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, t);
        }
    }
}

