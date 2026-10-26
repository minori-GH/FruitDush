using UnityEngine;
using System.Collections;

//このオブジェクトにはRigidbody2D必須ですよという宣言。これがあると自動でRigidbody2Dが追加される。すでにアタッチしていればなくても動くコードだが、パッと見たときに物理を使うことが分かるし、バグの防止になるので書いておく。
[RequireComponent(typeof(Rigidbody2D))]

//（public）Unityや他のスクリプトからもアクセスできる（class）設計図を作ります（　）このスクリプトの：（MonoBehaviour）Unityで
public class PlayerController : MonoBehaviour
{
    //（[SerializeField] private）外からは触れないがUnity上では調整できる（float）小数（runSpeed/jumoForce）走る速さ/ジャンプの高さ
    [Header("Move")]
    [SerializeField] private float runSpeed = 4f;
    [Header("Jump")]
    [SerializeField] private float jumpForce = 8f;

    [Header("Ground Check")]
    //地面チェックをする位置を表している。位置（Position）だけ使いたいときはTransformを使う。
    [SerializeField] private Transform groundCheck;
    //地面チェックの範囲(円の大きさ)を表している。
    [SerializeField] private float groundCheckRadius = 0.15f;
    //どれを地面として扱うかの指定。LayerでGroundLayerに指定したものが地面判定になる。
    [SerializeField] private LayerMask groundLayer;

    // 0.2秒以上で方向転換
    [Header("Long Press")]
    [SerializeField] private float longPressThreshold = 0.2f;

    // ===== 敵ヒット関連パラメータ =====
    [Header("Enemy Hit")]
    // 横方向ノックバック
    [SerializeField] private float knockbackX = 5f; 
    // 上方向ノックバック
    [SerializeField] private float knockbackY = 3f;
    // スタン時間
    [SerializeField] private float stunDuration = 0.5f;   
    // 無敵時間
    [SerializeField] private float invincibleDuration = 1f;
    // ===================================

    // GameManagerを参照する
    private GameManager gameManager;

    // Animator参照
    private Animator animator;

    //プレイヤーの物理挙動を操作するための本体
    private Rigidbody2D rb;
    //「地面にいるかどうか」の状態true → 地面にいる、false → 空中
    private bool isGrounded;

    // プレイヤーの状態フラグ。isStunned動けない、isInvincibleダメージ無効
    private bool isStunned = false;
    private bool isInvincible = false;

    // 処理の予約管理。連続ヒットしたときは前の処理を止めて上書きする。
    private Coroutine stunCoroutine;
    private Coroutine invincibleCoroutine;

    //押しているかどうか
    private bool isPressing = false;
    //押し始めた時間を記録
    private float pressStartTime = 0f;
    //移動方向を表す。1=右, -1=左
    private int moveDirection = 1;


    //ゲーム開始時に一度だけ実行する。Startより先に呼ばれる。「自分の部品を揃える、呼び起こす」みたいなイメージ。
    private void Awake()
    {
        //rb =物理操作の準備同じオブジェクトについている。Rigidbody2Dを取得する。自動で取ってこれないので指定してあげる必要がある。
        rb = GetComponent<Rigidbody2D>();

        // GameManagerを取得
        gameManager = FindFirstObjectByType<GameManager>();

        animator = GetComponent<Animator>();

        //「まだ設定されてないなら」
        if (groundCheck == null)
        {
            //自分の子の中から「GroundCheck」という名前を探す
            Transform found = transform.Find("GroundCheck");
            //見つかった場合だけ設定
            if (found != null) groundCheck = found;
        }
    }

    //入力はUpdateで取るのが基本
    private void Update()
    {
        // ★変更: 毎フレーム地面判定を更新（Animatorにも使う）
        isGrounded = CheckGrounded();

        // ★変更: Playing状態かどうかを先に取得
        bool isPlaying = IsGamePlaying();

        // ★変更: Animatorパラメータ更新（running / jumping / stunned）
        UpdateAnimatorParameters(isPlaying);

        // 変更: Playing状態以外では入力させない
        if (!IsGamePlaying())
        {
            return;
        }

        // スタン中は入力無効
        if (!CanReceiveInput()) return;

        //タップ/長押しを共通処理
        HandleTapAndLongPressInput();
    }

    //物理更新のタイミングで定期的に呼ばれる処理
    //物理計算と同期させるためにRigidbody操作はFixedUpdateが基本ルール
    private void FixedUpdate()
    {
        // Playing状態以外では移動しない
        if (!IsGamePlaying())
        {
            return;
        }

        // スタン中
        if (!CanMove())
        {
            // 地面にいる時だけ横速度を減衰
            if (isGrounded)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x * 0.8f,
                    rb.linearVelocity.y
                );
            }

            return;
        }

        //（rb.velocity）現在の速度（new Vector2）新しい速度を作る（runSpeed）
        rb.linearVelocity = new Vector2(runSpeed * moveDirection, rb.linearVelocity.y);
    }

    // GameManagerの状態を確認する関数を追加
    private bool IsGamePlaying()
    {
        // nullチェック
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return false;
            }
        }

        return gameManager.CurrentState == GameManager.GameState.Playing;
    }

    //地面にいるかどうかをtrueかfalseで返す関数
    private bool CheckGrounded()
    {
        //エラーを防ぐために、groundCheckが未設定ならとりあえず空中扱いにする
        if (groundCheck == null) return false;

        //（OverlapCircle）指定した位置に円を作って、何かに当たっているか調べる
        return Physics2D.OverlapCircle(
            groundCheck.position,//円の中心
            groundCheckRadius,//円の大きさ
            groundLayer//どのレイヤーを「地面」とみなすか
        );
    }

    //タップと長押しを判定する関数
    private void HandleTapAndLongPressInput()
    {
        // 押し始め
        if (IsPointerDownThisFrame())
        {
            //Time.time=ゲーム開始からの経過時間
            isPressing = true;
            pressStartTime = Time.time;
        }

        // 離した瞬間　&&ちゃんと押していたを追加することで誤作動防止
        if (IsPointerUpThisFrame() && isPressing)
        {
            // 押した時間を計算
            isPressing = false;
            float pressDuration = Time.time - pressStartTime;

            // 0.2秒以上なら方向転換
            if (pressDuration >= longPressThreshold)
            {
                TurnAround();
            }
            // 0.2秒未満ならジャンプ（地面にいる時のみ）
            else if (isGrounded)
            {
                Jump();
            }
        }
    }

    // 押した瞬間を検出（モバイル＋PC）
    private bool IsPointerDownThisFrame()
    {
        // モバイル：指が触れた瞬間（Began）
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) return true;
        }

        // PC：クリックした瞬間
        return Input.GetMouseButtonDown(0);
    }
    
    // 離した瞬間を検出（モバイル＋PC）
    private bool IsPointerUpThisFrame()
    {
        // モバイル：指が離れた瞬間
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) return true;
        }

        // PC：クリックを離した瞬間
        return Input.GetMouseButtonUp(0);
    }
    
    private void Jump()
    {
        // ジャンプ高さを安定させるためY速度をリセット
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // 上向きに力を加える
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // ジャンプSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayJumpSE();
        }
    }

    // 方向転換処理
    private void TurnAround()
    {
        // 進行方向に-1を掛ける
        moveDirection *= -1;

        // 見た目の向きも反転（Spriteを左右反転）
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * moveDirection;
        transform.localScale = scale;
    }

    // EnemyContact から呼ばれる関数
    public void OnEnemyHit(Vector2 enemyWorldPosition)
    {
        // 無敵中は無視
        if (isInvincible)
        {
            return;
        }

        // Hit効果音ならす
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEnemyHitSE();
        }

        // 敵から離れる方向を計算
        Vector2 away = (Vector2)transform.position - enemyWorldPosition;
        // 同じ位置だった場合は右にとばす
        if (away.sqrMagnitude < 0.0001f)
        {
            away = Vector2.right;
        }
        away.Normalize();

        // ノックバック（横＋上）
        rb.linearVelocity = new Vector2(away.x * knockbackX, knockbackY);

        // スタン開始（0.5秒）
        if (stunCoroutine != null) StopCoroutine(stunCoroutine);
        stunCoroutine = StartCoroutine(StunRoutine());

        // 無敵開始（1秒）
        if (invincibleCoroutine != null) StopCoroutine(invincibleCoroutine);
        invincibleCoroutine = StartCoroutine(InvincibleRoutine());
    }

    // スタン時間管理
    private IEnumerator StunRoutine()
    {
        isStunned = true;
        yield return new WaitForSeconds(stunDuration);
        isStunned = false;
        stunCoroutine = null;
    }

    // 無敵時間管理
    private IEnumerator InvincibleRoutine()
    {
        isInvincible = true;
        yield return new WaitForSeconds(invincibleDuration);
        isInvincible = false;
        invincibleCoroutine = null;
    }

    // 制御用関数
    private bool CanMove()
    {
        return !isStunned;
    }

    private bool CanReceiveInput()
    {
        return !isStunned;
    }

    public bool IsStunned()
    {
        return isStunned;
    }

    // CameraFollow2D から進行方向を取得できるように追加
    public int GetMoveDirection()
    {
        return moveDirection;
    }

    // ★変更: AnimatorのBoolパラメータ更新（running / jumping / stunned）
    private void UpdateAnimatorParameters(bool isPlaying)
    {
        // nullチェック
        if (animator == null || rb == null)
        {
            return;
        }

        // 横移動速度の絶対値
        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);

        // running: 走行中のみtrue（停止時false、空中時false、スタン時false）
        bool running = isPlaying && isGrounded && !isStunned && horizontalSpeed > 0.1f;

        // jumping: 空中ならtrue、地上ならfalse
        bool jumping = !isGrounded;

        // stunned: スタン中true
        bool stunned = isStunned;

        animator.SetBool("running", running);
        animator.SetBool("jumping", jumping);
        animator.SetBool("stunned", stunned);
    }

    //デバック用！！
    //オブジェクトを選択しているときだけ呼ばれる特別な関数。Sceneビューにだけ表示され、Gameビューには出ない。
    private void OnDrawGizmosSelected()
    {
        //未設定なら何も描かない。これがないとエラーの原因に
        if (groundCheck == null) return;
        //これから描く線の色を黄色にする
        Gizmos.color = Color.yellow;
        //ワイヤーフレームの円を描く
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}