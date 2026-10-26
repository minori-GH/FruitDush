using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

// Localizationの現在言語に応じて、同じGameObject上のTextMeshProUGUIフォントを切り替えるスクリプト
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTMPFontSwitcher : MonoBehaviour
{
    [Header("Language Fonts")]
    [SerializeField] private TMP_FontAsset japaneseFont;           // ja
    [SerializeField] private TMP_FontAsset englishFont;            // en
    [SerializeField] private TMP_FontAsset chineseSimplifiedFont;  // zh-Hans
    [SerializeField] private TMP_FontAsset chineseTraditionalFont; // zh-Hant
    [SerializeField] private TMP_FontAsset koreanFont;             // ko

    [Header("Fallback Font (fr, de, es, pt, ru, vi, etc...)")]
    [SerializeField] private TMP_FontAsset commonFallbackFont;

    private TextMeshProUGUI targetText;

    private void Awake()
    {
        // このスクリプトが付いている同じGameObjectのTextMeshProUGUIを取得
        targetText = GetComponent<TextMeshProUGUI>();

        if (targetText == null)
        {
            Debug.LogWarning("[LocalizedTMPFontSwitcher] TextMeshProUGUI が見つかりません。");
        }
    }

    private void OnEnable()
    {
        // 言語変更イベントを登録
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;

        // 有効化時に現在言語で即時適用
        ApplyFont(LocalizationSettings.SelectedLocale);
    }

    private void OnDisable()
    {
        // イベント解除（メモリリーク防止）
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        ApplyFont(locale);
    }

    private void ApplyFont(Locale locale)
    {
        if (targetText == null)
        {
            return;
        }

        if (locale == null || locale.Identifier.Code == null)
        {
            // Localeが取れない場合は共通フォントを使う（設定されていれば）
            SetFontIfValid(commonFallbackFont);
            return;
        }

        string code = locale.Identifier.Code; // 例: ja, en, zh-Hans, zh-Hant, ko
        TMP_FontAsset selectedFont = GetFontByLocaleCode(code);

        SetFontIfValid(selectedFont);
    }

    private TMP_FontAsset GetFontByLocaleCode(string localeCode)
    {
        // 安全のため小文字比較しやすくする（zh-Hans/zh-Hant判定は元文字列も使う）
        string lc = localeCode.ToLowerInvariant();

        // 日本語
        if (lc == "ja" || lc.StartsWith("ja-"))
        {
            return japaneseFont != null ? japaneseFont : commonFallbackFont;
        }

        // 英語
        if (lc == "en" || lc.StartsWith("en-"))
        {
            return englishFont != null ? englishFont : commonFallbackFont;
        }

        // 中国語（簡体）
        if (localeCode == "zh-Hans" || lc == "zh-cn" || lc.StartsWith("zh-hans"))
        {
            return chineseSimplifiedFont != null ? chineseSimplifiedFont : commonFallbackFont;
        }

        // 中国語（繁体）
        if (localeCode == "zh-Hant" || lc == "zh-tw" || lc.StartsWith("zh-hant"))
        {
            return chineseTraditionalFont != null ? chineseTraditionalFont : commonFallbackFont;
        }

        // 韓国語
        if (lc == "ko" || lc.StartsWith("ko-"))
        {
            return koreanFont != null ? koreanFont : commonFallbackFont;
        }

        // その他（fr, de, es, pt, ru, vi など）
        return commonFallbackFont;
    }

    private void SetFontIfValid(TMP_FontAsset fontAsset)
    {
        if (fontAsset == null)
        {
            Debug.LogWarning("[LocalizedTMPFontSwitcher] 適用するフォントが未設定です。");
            return;
        }

        // 同じフォントなら再設定しない
        if (targetText.font == fontAsset)
        {
            return;
        }

        targetText.font = fontAsset;
        // 見た目更新を確実にする
        targetText.SetAllDirty();
    }
}