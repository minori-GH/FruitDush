using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 現在スコア
    private int currentScore = 0;

    // タイマー（秒）
    [SerializeField] private float timeRemaining = 60f;

    // ===== 変更: ゲーム状態enumを追加 =====
    public enum GameState
    {
        Start,
        Playing,
        Pause,
        GameOver,
        GameClear
    }

    // ===== 変更: 現在の状態を保持する変数を追加 =====
    [SerializeField] private GameState currentState = GameState.Start;
    public GameState CurrentState => currentState;

    private bool isGameOverStarted = false;

    // リワード復活は1回のみ
    private bool hasUsedRewardContinue = false;

    // メインスレッド(Update)で復活処理を行うための予約データ
    private bool pendingRewardResume = false;
    private float pendingRewardSeconds = 0f;
    private readonly object rewardLock = new object();

    private System.Collections.IEnumerator GameOverNextFrame()
    {
        if (isGameOverStarted)
        {
            yield break;
        }

        isGameOverStarted = true;

        // 1フレーム待つ
        yield return null;

        OnGameOver();
    }

    private void Start()
    {
        // ===== 変更: Time.timeScaleの初期化もGameManagerで管理 =====
        Time.timeScale = 1f;
    }

    private void Update()
    {
        // まず復活予約を処理（GameOver中でも動く）
        ProcessPendingRewardResume();

        // Playing状態のときだけタイマーを進める
        if (currentState != GameState.Playing)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        // 負の値にならないように補正
        if (timeRemaining < 0f)
        {
            timeRemaining = 0f;
        }

        Debug.Log("Time Remaining: " + timeRemaining.ToString("F2"));

        // 0秒になったら、
        // そのフレームではまだPlaying維持
        // 次フレームでGameOverにする
        if (timeRemaining <= 0f)
        {
            StartCoroutine(GameOverNextFrame());
        }
    }

    // メインスレッドでのみUnity API(Time.timeScale等)を触る
    private void ProcessPendingRewardResume()
    {
        bool shouldResume = false;
        float addSeconds = 0f;

        lock (rewardLock)
        {
            if (pendingRewardResume)
            {
                shouldResume = true;
                addSeconds = pendingRewardSeconds;

                pendingRewardResume = false;
                pendingRewardSeconds = 0f;
            }
        }

        if (!shouldResume)
        {
            return;
        }

        // 実行時点で条件再確認
        if (!CanUseRewardContinue())
        {
            Debug.LogWarning("[GameManager] Pending reward resume canceled. State changed.");
            return;
        }

        AddTime(addSeconds);
        hasUsedRewardContinue = true;
        isGameOverStarted = false;
        currentState = GameState.Playing;

        // Time.timeScaleは必ずメインスレッド(Update)で変更
        Time.timeScale = 1f;

        Debug.Log("[GameManager] Reward continue applied on main thread. +" + addSeconds + " sec");
    }

    public void AddScore(int value)
    {
        if (value <= 0)
        {
            Debug.LogWarning("AddScore: value は 1 以上を指定してください。");
            return;
        }

        currentScore += value;
        Debug.Log("Current Score: " + currentScore);
    }

    // ===== 変更: 状態変更関数を追加 =====
    public void StartGame()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        isGameOverStarted = false;//★？？？
        hasUsedRewardContinue = false; //リワード広告回数リセット
        Debug.Log("State: Playing");
    }

    public void PauseGame()
    {
        if (currentState != GameState.Playing) return;

        currentState = GameState.Pause;
        Time.timeScale = 0f;
        Debug.Log("State: Pause");
    }

    public void ResumeGame()
    {
        if (currentState != GameState.Pause) return;

        currentState = GameState.Playing;
        Time.timeScale = 1f;
        Debug.Log("State: Playing (Resume)");
    }

    public void OnGameOver()
    {
        if (currentState == GameState.GameOver) return;

        // ゲームオーバーSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameOverSE();
        }

        currentState = GameState.GameOver;
        Time.timeScale = 0f;

        Debug.Log("GAME OVER");
    }

    public void OnGameClear()
    {
        if (currentState == GameState.GameClear) return;

        // ゲームクリアSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGameClearSE();
        }

        currentState = GameState.GameClear;
        Time.timeScale = 0f;

        Debug.Log("GAME CLEAR!");
    }

    // ★変更: リワード報酬用（残り時間を追加）
    public void AddTime(float value)
    {
        if (value <= 0f)
        {
            Debug.LogWarning("AddTime: value は 0 より大きくしてください。");
            return;
        }

        timeRemaining += value;
        Debug.Log("Time Added: +" + value + "s  => " + timeRemaining.ToString("F2"));
    }

    // 復活可能かどうか
    public bool CanUseRewardContinue()
    {
        return currentState == GameState.GameOver && !hasUsedRewardContinue;
    }

    // ここでは予約フラグを立てるだけ（Unity APIを触らない）
    public void ApplyRewardContinue(float addSeconds)
    {
        if (addSeconds <= 0f)
        {
            Debug.LogWarning("[GameManager] ApplyRewardContinue: addSeconds must be > 0.");
            return;
        }

        if (!CanUseRewardContinue())
        {
            Debug.LogWarning("[GameManager] Reward continue is not allowed now.");
            return;
        }

        lock (rewardLock)
        {
            pendingRewardResume = true;
            pendingRewardSeconds = addSeconds;
        }

        Debug.Log("[GameManager] Reward continue queued.");
    }

    // UI表示用Getter
    public float GetTimeRemaining()
    {
        return timeRemaining;
    }

    public int GetScore()
    {
        return currentScore;
    }
}