using System.Collections;
using TMPro;
using UnityEngine;

// GameManagerの残り時間と状態を見て、UI演出を実行するスクリプト
// ・30秒以下で時計UIを1回だけポップ演出
// ・5秒以下でカウントダウン（5→1）を1回だけ表示
public class TimeUIEffectController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;      // 残り時間と状態を取得する先
    [SerializeField] private RectTransform clockUI;        // 拡大・縮小する時計UI
    [SerializeField] private GameObject countdownUI;       // カウントダウンUIの親オブジェクト
    [SerializeField] private TextMeshProUGUI countdownText; // カウントダウン数字表示

    [Header("Clock Pop Settings")]
    [SerializeField] private float popScale = 1.25f;       // どのくらい拡大するか
    [SerializeField] private float popDuration = 0.18f;    // 演出全体時間（秒）

    // 一度だけ実行するためのフラグ
    private bool hasPlayed30SecPop = false;
    private bool hasStarted5SecCountdown = false;

    // 元のサイズを保存（演出後に戻すため）
    private Vector3 originalClockScale = Vector3.one;

    private void Awake()
    {
        // GameManagerが未設定なら自動で探す
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
        }

        // clockUIがある場合は元のサイズを記録
        if (clockUI != null)
        {
            originalClockScale = clockUI.localScale;
        }

        // カウントダウンUIは初期で非表示
        if (countdownUI != null)
        {
            countdownUI.SetActive(false);
        }
    }

    private void Update()
    {
        // nullチェック
        if (gameManager == null)
        {
            return;
        }

        // Playing中だけ処理する
        if (gameManager.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        // GameManagerから現在の残り時間を取得
        float timeRemaining = gameManager.GetTimeRemaining();

        // 30秒以下になった瞬間に1回だけポップ演出
        if (!hasPlayed30SecPop && timeRemaining <= 30f)
        {
            hasPlayed30SecPop = true;
            StartCoroutine(PlayClockPopEffect());
        }

        // 5秒以下になった瞬間に1回だけカウントダウン開始
        if (!hasStarted5SecCountdown && timeRemaining <= 5f)
        {
            hasStarted5SecCountdown = true;
            StartCoroutine(PlayCountdown5To1());
        }
    }

    // 時計UIを「拡大→元に戻す」演出
    private IEnumerator PlayClockPopEffect()
    {
        if (clockUI == null)
        {
            yield break;
        }

        Vector3 startScale = originalClockScale;
        Vector3 expandedScale = originalClockScale * popScale;

        // 前半：拡大
        float half = popDuration * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float rate = Mathf.Clamp01(t / half);
            clockUI.localScale = Vector3.Lerp(startScale, expandedScale, rate);
            yield return null;
        }

        // 後半：元に戻す
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float rate = Mathf.Clamp01(t / half);
            clockUI.localScale = Vector3.Lerp(expandedScale, startScale, rate);
            yield return null;
        }

        // 最後に念のため元サイズに固定
        clockUI.localScale = originalClockScale;
    }

    // カウントダウンUIを表示して 5→4→3→2→1 を1秒ごとに表示
    private IEnumerator PlayCountdown5To1()
    {
        if (countdownUI == null || countdownText == null)
        {
            yield break;
        }

        countdownUI.SetActive(true);

        int lastValue = -1;

        while (gameManager != null &&
               gameManager.CurrentState == GameManager.GameState.Playing)
        {
            float timeRemaining = gameManager.GetTimeRemaining();

            // 切り上げで表示値を決定
            int displayValue;

            if (timeRemaining <= 0f)
            {
                displayValue = 0;
            }
            else
            {
                displayValue = Mathf.CeilToInt(timeRemaining);
            }

            // 5～0だけ表示
            if (displayValue <= 5 && displayValue >= 0)
            {
                // 値が変わった瞬間だけ更新
                if (displayValue != lastValue)
                {
                    countdownText.text = displayValue.ToString();
                    lastValue = displayValue;
                }
            }

            // 0になったら終了
            if (displayValue <= 0)
            {
                break;
            }

            yield return null;
        }

        countdownUI.SetActive(false);
    }
}