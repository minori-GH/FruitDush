using System.Collections;
using TMPro;
using UnityEngine;

// リザルト表示時にベストスコアを更新・表示するスクリプト
public class ResultBestScoreUI : MonoBehaviour
{
    // PlayerPrefsに保存するキー名
    private const string BestScoreKey = "BEST_SCORE";

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private GameObject newRecordUI;

    // 同じ表示中に複数回処理しないためのフラグ
    private bool hasProcessed = false;

    // ★修正: GameClear待機用コルーチン参照
    private Coroutine waitForClearCoroutine;

    private void OnEnable()
    {
        // リセット
        hasProcessed = false;

        // GameManagerが未設定なら取得
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // nullなら何もしない
        if (gameManager == null)
        {
            Debug.LogWarning("ResultBestScoreUI: GameManager が見つかりません。");
            return;
        }

        // ★修正: OnEnable時にGameClearなら即実行、そうでなければ待機
        if (gameManager.CurrentState == GameManager.GameState.GameClear)
        {
            ApplyBestScoreForGameClear();
        }
        else
        {
            // StartUI / Playing中は処理しない（GameClearになるまで待つ）
            waitForClearCoroutine = StartCoroutine(WaitForGameClearThenApply());
        }
    }

    private void OnDisable()
    {
        // ★修正: 無効化時に待機コルーチンを停止
        if (waitForClearCoroutine != null)
        {
            StopCoroutine(waitForClearCoroutine);
            waitForClearCoroutine = null;
        }
    }

    /// 外部から明示的に呼びたい場合用（任意）
    public void RefreshResult()
    {
        // ★修正: 明示呼び出しでもGameClear時のみ実行
        ApplyBestScoreForGameClear();
    }

    // ★修正: GameClearになるまで待ってから1回だけ実行
    private IEnumerator WaitForGameClearThenApply()
    {
        while (true)
        {
            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
                if (gameManager == null)
                {
                    yield return null;
                    continue;
                }
            }

            if (gameManager.CurrentState == GameManager.GameState.GameClear)
            {
                ApplyBestScoreForGameClear();
                yield break;
            }

            yield return null;
        }
    }

    // ★修正: GameClear時専用の更新処理
    private void ApplyBestScoreForGameClear()
    {
        // 1回だけ実行
        if (hasProcessed)
        {
            return;
        }

        // nullチェック
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogWarning("ResultBestScoreUI: GameManager が見つかりません。");
                return;
            }
        }

        // ★修正: GameClear時のみ処理する
        if (gameManager.CurrentState != GameManager.GameState.GameClear)
        {
            return;
        }

        // 現在スコアを取得
        int currentScore = gameManager.GetScore();

        // ベストスコア取得（未保存時は0）
        int bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        // 更新判定
        bool isNewRecord = currentScore > bestScore;
        if (isNewRecord)
        {
            bestScore = currentScore;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }

        // ★修正: Saveは必ず呼ぶ（確実保存）
        PlayerPrefs.Save();

        // UI反映
        if (bestScoreText != null)
        {
            bestScoreText.text = bestScore.ToString();
        }

        if (newRecordUI != null)
        {
            newRecordUI.SetActive(isNewRecord);
        }

        hasProcessed = true;
    }
}