using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    // Singleton（どこからでも AudioManager.Instance で呼べる）
    public static AudioManager Instance { get; private set; }

    [Header("SE AudioSource")]
    // SE再生専用のAudioSource
    [SerializeField] private AudioSource seSource;

    [Header("SE Clips")]
    [SerializeField] private AudioClip enemyHitSE;   // ① 敵ヒット
    [SerializeField] private AudioClip itemGetSE;    // ② アイテム取得
    [SerializeField] private AudioClip jumpSE;       // ③ ジャンプ
    [SerializeField] private AudioClip gameOverSE;   // ④ ゲームオーバー
    [SerializeField] private AudioClip gameClearSE;  // ⑤ ゲームクリア
    [SerializeField] private AudioClip buttonClickSE;// ⑥ ボタン押下

    // 各SEごとの個別音量（0〜1）
    [Header("SE Volumes (0 to 1)")]
    [Range(0f, 1f)][SerializeField] private float enemyHitVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float itemGetVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float jumpVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float gameOverVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float gameClearVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float buttonVolume = 1f;

    private void Awake()
    {
        // Singletonの重複防止
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // AudioSourceの取得（未設定でも自動で取得）
        if (seSource == null)
        {
            seSource = GetComponent<AudioSource>();
        }

        if (seSource == null)
        {
            Debug.LogWarning("AudioManager: AudioSource が見つかりません。");
            return;
        }

        seSource.playOnAwake = false;
    }

    // 汎用SE再生関数
    public void PlaySE(AudioClip clip, float volume)
    {
        if (seSource == null)
        {
            Debug.LogWarning("AudioManager: seSource が null です。");
            return;
        }

        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 再生する AudioClip が未設定です。");
            return;
        }

        // 念のため 0〜1 に丸める
        float safeVolume = Mathf.Clamp01(volume);

        // PlayOneShot(clip, volume) で再生
        seSource.PlayOneShot(clip, safeVolume);

    }

    // 各SEを対応する個別音量で再生
    public void PlayEnemyHitSE()
    {
        PlaySE(enemyHitSE, enemyHitVolume);
    }

    public void PlayItemGetSE()
    {
        PlaySE(itemGetSE, itemGetVolume);
    }

    public void PlayJumpSE()
    {
        PlaySE(jumpSE, jumpVolume);
    }

    public void PlayGameOverSE()
    {
        PlaySE(gameOverSE, gameOverVolume);
    }

    public void PlayGameClearSE()
    {
        PlaySE(gameClearSE, gameClearVolume);
    }

    public void PlayButtonClickSE()
    {
        PlaySE(buttonClickSE, buttonVolume);
    }
}