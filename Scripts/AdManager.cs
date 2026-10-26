// AdMobManager.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdMobManager : MonoBehaviour
{
    // どこからでも使えるようにSingleton化
    public static AdMobManager Instance { get; private set; }

    [Header("Ad Unit IDs (Test IDs are set by default)")]
    [SerializeField] private string iosInterstitialAdUnitId = "ca-app-pub-3940256099942544/4411468910";
    [SerializeField] private string androidInterstitialAdUnitId = "ca-app-pub-3940256099942544/1033173712";

    [Header("Rewarded Ad Unit IDs (Test IDs)")]
    [SerializeField] private string iosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    ///修正: 本番広告ID運用時でも安全に実機テストできるよう、テスト端末IDをInspectorで管理
    ///修正: ここに自分の端末IDを入れると、その端末では "Test Ad" が表示される
    [Header("AdMob Test Device IDs")]
    [SerializeField]
    private List<string> testDeviceIds = new List<string>
    {
        "YOUR_TEST_DEVICE_ID"
    };

    private InterstitialAd interstitialAd;
    private RewardedAd rewardedAd;

    private bool isInitialized = false;
    private Action onAdClosedAction;

    // Rewarded処理の一時保持データ
    private bool rewardEarnedThisShow = false;
    private bool rewardClosedThisShow = false;
    private bool rewardApplyQueued = false;

    private GameManager pendingRewardGameManager = null;
    private float pendingRewardSeconds = 10f;

    ///修正: AdMobコールバックはメインスレッド保証がない場合があるため、
    ///修正: Unity API呼び出しはUpdateで実行するキュー方式にする
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    private readonly object mainThreadLock = new object();

    private void Awake()
    {
        // Singleton重複防止
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeAdMob();
    }

    ///修正: Unity APIを触る処理はここで実行（メインスレッド）
    private void Update()
    {
        ExecuteMainThreadActions();
    }

    private void ExecuteMainThreadActions()
    {
        while (true)
        {
            Action action = null;

            lock (mainThreadLock)
            {
                if (mainThreadActions.Count > 0)
                {
                    action = mainThreadActions.Dequeue();
                }
            }

            if (action == null)
            {
                break;
            }

            action.Invoke();
        }
    }

    private void EnqueueMainThread(Action action)
    {
        if (action == null) return;

        lock (mainThreadLock)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    public void InitializeAdMob()
    {
        if (isInitialized)
        {
            return;
        }

        ///修正: MobileAds.Initialize の前にテスト端末設定を適用する
        ///修正: これにより、本番広告IDを使っていても指定端末ではテスト広告として配信される
        var requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = testDeviceIds
        };
        MobileAds.SetRequestConfiguration(requestConfiguration);

        MobileAds.Initialize(initStatus =>
        {
            // 初期化完了後の処理もメインスレッドキューに寄せる
            EnqueueMainThread(() =>
            {
                isInitialized = true;
                Debug.Log("[AdMobManager] SDK initialized.");

                LoadInterstitial();
                LoadRewarded();
            });
        });
    }

    private string GetInterstitialAdUnitId()
    {
#if UNITY_IOS
        _ = androidInterstitialAdUnitId;
        return iosInterstitialAdUnitId;
#elif UNITY_ANDROID
        _ = iosInterstitialAdUnitId;
        return androidInterstitialAdUnitId;
#else
        _ = iosInterstitialAdUnitId;
        _ = androidInterstitialAdUnitId;
        return iosInterstitialAdUnitId;
#endif
    }

    private string GetRewardedAdUnitId()
    {
#if UNITY_IOS
        _ = androidRewardedAdUnitId;
        return iosRewardedAdUnitId;
#elif UNITY_ANDROID
        _ = iosRewardedAdUnitId;
        return androidRewardedAdUnitId;
#else
        _ = iosRewardedAdUnitId;
        _ = androidRewardedAdUnitId;
        return iosRewardedAdUnitId;
#endif
    }

    public void LoadInterstitial()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[AdMobManager] SDK not initialized yet.");
            return;
        }

        string adUnitId = GetInterstitialAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdMobManager] Interstitial Ad Unit ID is empty.");
            return;
        }

        DestroyInterstitial();

        AdRequest request = new AdRequest();

        InterstitialAd.Load(adUnitId, request, (ad, error) =>
        {
            EnqueueMainThread(() =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[AdMobManager] Interstitial load failed: " + error);
                    return;
                }

                interstitialAd = ad;
                RegisterInterstitialEvents(interstitialAd);
                Debug.Log("[AdMobManager] Interstitial loaded.");
            });
        });
    }

    public void LoadRewarded()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("[AdMobManager] SDK not initialized yet.");
            return;
        }

        string adUnitId = GetRewardedAdUnitId();
        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdMobManager] Rewarded Ad Unit ID is empty.");
            return;
        }

        DestroyRewarded();

        AdRequest request = new AdRequest();
        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            EnqueueMainThread(() =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogWarning("[AdMobManager] Rewarded load failed: " + error);
                    return;
                }

                rewardedAd = ad;
                RegisterRewardedEvents(rewardedAd);
                Debug.Log("[AdMobManager] Rewarded loaded.");
            });
        });
    }

    private void RegisterInterstitialEvents(InterstitialAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            EnqueueMainThread(() =>
            {
                Debug.Log("[AdMobManager] Interstitial opened.");
            });
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            EnqueueMainThread(() =>
            {
                Debug.Log("[AdMobManager] Interstitial closed.");

                DestroyInterstitial();
                LoadInterstitial();

                Action action = onAdClosedAction;
                onAdClosedAction = null;
                action?.Invoke();
            });
        };

        ad.OnAdFullScreenContentFailed += (adError) =>
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("[AdMobManager] Interstitial show failed: " + adError);

                DestroyInterstitial();
                LoadInterstitial();

                Action action = onAdClosedAction;
                onAdClosedAction = null;
                action?.Invoke();
            });
        };
    }

    private void RegisterRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentOpened += () =>
        {
            EnqueueMainThread(() =>
            {
                Debug.Log("[AdMobManager] Rewarded opened.");
            });
        };

        ad.OnAdFullScreenContentClosed += () =>
        {
            // Closeが先に来る場合もあるのでフラグだけ立てる
            rewardClosedThisShow = true;

            EnqueueMainThread(() =>
            {
                Debug.Log("[AdMobManager] Rewarded closed.");

                ///修正: 「報酬取得」と「Close」の両方が揃ったときだけ復活予約を実行
                TryApplyRewardWhenReadyOnMainThread();

                // 次回用に広告はすぐ再ロード
                DestroyRewarded();
                LoadRewarded();
            });
        };

        ad.OnAdFullScreenContentFailed += (adError) =>
        {
            EnqueueMainThread(() =>
            {
                Debug.LogWarning("[AdMobManager] Rewarded show failed: " + adError);

                ClearRewardFlowState();
                DestroyRewarded();
                LoadRewarded();
            });
        };
    }

    ///修正: どちらのコールバック順でも安全に処理できるように分離
    private void TryApplyRewardWhenReadyOnMainThread()
    {
        if (rewardApplyQueued) return;

        if (rewardEarnedThisShow && rewardClosedThisShow)
        {
            rewardApplyQueued = true;

            if (pendingRewardGameManager != null)
            {
                pendingRewardGameManager.ApplyRewardContinue(pendingRewardSeconds);
                Debug.Log("[AdMobManager] Reward request sent to GameManager on main thread.");
            }
            else
            {
                Debug.LogWarning("[AdMobManager] Reward target GameManager is null.");
            }

            ClearRewardFlowState();
        }
    }

    private void ClearRewardFlowState()
    {
        rewardEarnedThisShow = false;
        rewardClosedThisShow = false;
        rewardApplyQueued = false;
        pendingRewardGameManager = null;
        pendingRewardSeconds = 10f;
    }

    public void ShowInterstitial(Action onClosed)
    {
        onAdClosedAction = onClosed;

        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            interstitialAd.Show();
        }
        else
        {
            Debug.LogWarning("[AdMobManager] Interstitial is not ready. Skip ad.");

            Action action = onAdClosedAction;
            onAdClosedAction = null;
            action?.Invoke();

            LoadInterstitial();
        }
    }

    ///修正: 表示開始できたかをboolで返す（UI側でボタン状態制御しやすくする）
    public bool ShowRewardedForExtraTime(GameManager gameManager, float addSeconds = 10f)
    {
        if (gameManager == null)
        {
            Debug.LogWarning("[AdMobManager] GameManager is null.");
            return false;
        }

        if (!gameManager.CanUseRewardContinue())
        {
            Debug.LogWarning("[AdMobManager] Reward continue is not available.");
            return false;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("[AdMobManager] Rewarded is not ready.");
            LoadRewarded();
            return false;
        }

        // 新しいRewardフロー開始
        rewardEarnedThisShow = false;
        rewardClosedThisShow = false;
        rewardApplyQueued = false;
        pendingRewardGameManager = gameManager;
        pendingRewardSeconds = addSeconds;

        rewardedAd.Show((Reward reward) =>
        {
            // 報酬獲得イベント（この時点では再開しない）
            rewardEarnedThisShow = true;

            // もしClose済みなら、このタイミングで適用される
            EnqueueMainThread(() =>
            {
                Debug.Log("[AdMobManager] Reward earned callback.");
                TryApplyRewardWhenReadyOnMainThread();
            });
        });

        return true;
    }

    public void DestroyInterstitial()
    {
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }
    }

    public void DestroyRewarded()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    private void OnDestroy()
    {
        DestroyInterstitial();
        DestroyRewarded(); ///修正: Rewardedも確実に破棄
    }
}