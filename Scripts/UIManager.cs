using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private GameManager gameManager;

    [Header("UI Root Objects")]
    [SerializeField] private GameObject startUI;
    [SerializeField] private GameObject inGameUI;   
    [SerializeField] private GameObject pauseUI;   
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject gameClearUI;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButtonOnPause;
    [SerializeField] private Button retryButtonOnGameOver;
    [SerializeField] private Button retryButtonOnGameClear;
    [SerializeField] private Button rewardAdsButton;

    [Header("Texts")]
    [SerializeField] private TMP_Text timerText;          
    [SerializeField] private TMP_Text scoreText;         
    [SerializeField] private TMP_Text gameClearScoreText;

    // Change: Retry回数管理用
    private const string RetryCountKey = "RETRY_COUNT";
    [SerializeField] private int interstitialInterval = 10; // 10回に1回広告

    // 前フレームの状態を保存する。変わったときに検知できる。
    private GameManager.GameState lastState;
    // 初回だけ必ず実行する。これで2回目以降は変わったときだけ実行するようになる。
    private bool hasStateApplied = false;

    private void Awake()
    {
        // GameManagerが未設定なら自動検索
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // ボタン登録（nullチェックあり）
        if (startButton != null) startButton.onClick.AddListener(OnClickStart);
        if (pauseButton != null) pauseButton.onClick.AddListener(OnClickPause);
        if (resumeButton != null) resumeButton.onClick.AddListener(OnClickResume);

        // Change: 3つのRetryボタンをすべて同じ処理へ
        if (retryButtonOnPause != null) retryButtonOnPause.onClick.AddListener(OnClickRetry);
        if (retryButtonOnGameOver != null) retryButtonOnGameOver.onClick.AddListener(OnClickRetry);
        if (retryButtonOnGameClear != null) retryButtonOnGameClear.onClick.AddListener(OnClickRetry);

        if (rewardAdsButton != null) rewardAdsButton.onClick.AddListener(OnClickRewardAds);
    }

    // 毎フレーム、ゲームの状態を見てUIを更新する
    private void Update()
    {
        // GameManagerが無いなら処理をやめる
        // null → 何も入っていない状態
        if (gameManager == null)
        {
            // return → その場で関数を終了
            return;
        }

        // 「今のゲーム状態を取得してcurrentStateに入れる」
        GameManager.GameState currentState = gameManager.CurrentState;

        // 初回 or(||) 状態が変わったときだけ実行
        // !hasStateApplied→まだ一度も実行していない
        // currentState != lastState→状態が変わった
        if (!hasStateApplied || currentState != lastState)
        {
            // 状態に応じてUIを切り替える
            ApplyUIByState(currentState);

            // 状態(currentState)が、GameClear状態になったら
            if (currentState == GameManager.GameState.GameClear)
            {
                // GameClearScoreTextを表示する
                UpdateGameClearScoreText();
            }

            // 今回の状態を保存
            lastState = currentState;
            // 初回処理は終わったよというフラグ（これがあることで変わったときだけ処理するようになる）
            hasStateApplied = true;
        }

        // 状態(currentState)が、Playing状態になったら
        if (currentState == GameManager.GameState.Playing ||
    currentState == GameManager.GameState.GameOver)
        {
            UpdateInGameTexts();
        }

        ///修正: 念のための安全策
        ///修正: Playing状態なのにtimeScaleが0のままなら1へ戻して再開を保証
        if (currentState == GameManager.GameState.Playing && Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }

    private void ApplyUIByState(GameManager.GameState state)
    {
        // いったん全部OFF
        SetActiveSafe(startUI, false);
        SetActiveSafe(inGameUI, false);
        SetActiveSafe(pauseUI, false);
        SetActiveSafe(gameOverUI, false);
        SetActiveSafe(gameClearUI, false);

        // 要件どおり状態ごとに表示切り替え
        switch (state)
        {
            case GameManager.GameState.Start:
                SetActiveSafe(startUI, true);
                break;

            case GameManager.GameState.Playing:
                SetActiveSafe(inGameUI, true);
                break;

            case GameManager.GameState.Pause:
                SetActiveSafe(inGameUI, true);
                SetActiveSafe(pauseUI, true);
                break;

            case GameManager.GameState.GameOver:
                SetActiveSafe(inGameUI, true);
                SetActiveSafe(gameOverUI, true);

                ///修正: GameOver表示時にRewardボタンの表示可否を更新
                ///修正: 1回使用後はCanUseRewardContinue()がfalseなので非表示にする
                UpdateRewardButtonVisibility();
                break;

            case GameManager.GameState.GameClear:
                SetActiveSafe(gameClearUI, true);
                break;
        }
    }

    ///修正: Rewardボタン表示制御を分離して可読性を上げる
    private void UpdateRewardButtonVisibility()
    {
        if (rewardAdsButton == null) return;
        if (gameManager == null)
        {
            rewardAdsButton.gameObject.SetActive(false);
            return;
        }

        bool canShow = gameManager.CurrentState == GameManager.GameState.GameOver
                       && gameManager.CanUseRewardContinue();

        rewardAdsButton.gameObject.SetActive(canShow);
    }


    // プレイ中のUI（Timer / Score）を毎フレーム更新する
    private void UpdateInGameTexts()
    {
        if (gameManager == null) return;

        if (timerText != null)
        {
            // timerText.textに代入する（＝）ゲームマネージャーから残り時間を取得して
            // ToString("F0")→少数を四捨五入で整数表示に変換
            float timeRemaining = gameManager.GetTimeRemaining();

            int displayTime;

            if (timeRemaining <= 0f)
            {
                displayTime = 0;
            }
            else
            {
                displayTime = Mathf.CeilToInt(timeRemaining);
            }

            timerText.text = displayTime.ToString();
        }

        if (scoreText != null)
        {
            // scoreText.textに代入する（＝）ゲームマネージャーからスコアを取得して
            // ToString()→数値を文字列に変換
            scoreText.text = gameManager.GetScore().ToString();
        }
    }

    // GameClearUIの最終スコア更新
    // 毎フレーム更新する必要がないので↑と関数を分けている
    private void UpdateGameClearScoreText()
    {
        if (gameManager == null) return;

        if (gameClearScoreText != null)
        {
            gameClearScoreText.text = gameManager.GetScore().ToString();
        }
    }

    private void SetActiveSafe(GameObject obj, bool active)
    {
        if (obj != null)
        {
            obj.SetActive(active);
        }
    }

    // ===== ボタン処理 =====

    public void OnClickStart()
    {
        // ボタンクリックSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        if (gameManager == null) return;
        gameManager.StartGame();
    }

    public void OnClickPause()
    {
        // ボタンクリックSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        if (gameManager == null) return;
        gameManager.PauseGame();
    }

    public void OnClickResume()
    {
        // ボタンクリックSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        if (gameManager == null) return;
        gameManager.ResumeGame();
    }

    public void OnClickRewardAds()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("[UIManager] GameManager not found.");
                return;
            }
        }

        if (!gameManager.CanUseRewardContinue())
        {
            Debug.LogWarning("[UIManager] Reward continue is not available.");
            UpdateRewardButtonVisibility(); ///修正
            return;
        }

        if (AdMobManager.Instance == null)
        {
            Debug.LogWarning("[UIManager] AdMobManager not found.");
            return;
        }

        ///修正: 多重タップ防止（表示中は無効）
        if (rewardAdsButton != null)
        {
            rewardAdsButton.interactable = false;
        }

        ///修正: 表示開始できたかを確認。失敗時は再度押せるように戻す
        bool started = AdMobManager.Instance.ShowRewardedForExtraTime(gameManager, 10f);
        if (!started && rewardAdsButton != null)
        {
            rewardAdsButton.interactable = true;
        }
    }

    public void OnClickRetry()
    {
        // ボタンクリックSE再生
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayButtonClickSE();
        }

        int currentRetryCount = PlayerPrefs.GetInt(RetryCountKey, 0) + 1;
        PlayerPrefs.SetInt(RetryCountKey, currentRetryCount);
        PlayerPrefs.Save();

        bool shouldShowAd = (interstitialInterval > 0) && (currentRetryCount % interstitialInterval == 0);

        if (shouldShowAd && AdMobManager.Instance != null)
        {
            // 広告表示 → 閉じたらシーンリロード
            AdMobManager.Instance.ShowInterstitial(ReloadCurrentSceneToStart);
        }
        else
        {
            // 広告なし回は即リロード
            ReloadCurrentSceneToStart();
        }
    }

    // Change: Start状態へ戻すための共通処理（シーン再読み込み）
    private void ReloadCurrentSceneToStart()
    {
        Time.timeScale = 1f; // Pause中でも確実に戻す
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}