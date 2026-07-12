using System;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using UnityEngine;

public class RewardedAdManager : MonoBehaviour
{
    public static RewardedAdManager I { get; private set; }

    [Header("AdMob")]
    [SerializeField] private bool useTestAdUnit = true;
    [SerializeField] private string androidRewardedAdUnitId = "";
    [SerializeField] private string iosRewardedAdUnitId = "";

    [Header("Request Settings")]
    [SerializeField] private bool tagForChildDirectedTreatment = false;
    [SerializeField] private bool tagForUnderAgeOfConsent = false;
    [SerializeField] private bool useMaxAdContentRatingG = true;

    private RewardedAd rewardedAd;
    private bool isInitialized;
    private bool isLoading;
    private bool isShowing;
    private bool hasEarnedReward;
    private Action pendingRewardAction;
    private Action pendingFailedAction;

    private const string AndroidTestRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string IosTestRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";

    private void Awake()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeAds();
    }

    public void ShowRewardedAd(Action onRewarded, Action onFailed = null)
    {
        if (isShowing)
            return;

        pendingRewardAction = onRewarded;
        pendingFailedAction = onFailed;

        if (!isInitialized)
        {
            Debug.LogWarning("AdMob is not initialized yet.");
            pendingFailedAction?.Invoke();
            ClearPendingCallbacks();
            return;
        }

        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            Debug.LogWarning("Rewarded ad is not ready.");
            LoadRewardedAd();
            pendingFailedAction?.Invoke();
            ClearPendingCallbacks();
            return;
        }

        isShowing = true;
        hasEarnedReward = false;

        rewardedAd.Show((Reward reward) =>
        {
            Debug.Log($"Reward earned. Type={reward.Type}, Amount={reward.Amount}");
            hasEarnedReward = true;
        });
    }

    private void InitializeAds()
    {
        ApplyRequestConfiguration();

        MobileAds.Initialize(initStatus =>
        {
            isInitialized = true;
            Debug.Log("AdMob initialized.");
            LoadRewardedAd();
        });
    }

    private void ApplyRequestConfiguration()
    {
        var requestConfiguration = new RequestConfiguration();

        if (tagForChildDirectedTreatment)
        {
            requestConfiguration.TagForChildDirectedTreatment = TagForChildDirectedTreatment.True;
        }

        if (tagForUnderAgeOfConsent)
        {
            requestConfiguration.TagForUnderAgeOfConsent = TagForUnderAgeOfConsent.True;
        }

        if (useMaxAdContentRatingG)
        {
            requestConfiguration.MaxAdContentRating = MaxAdContentRating.G;
        }

        MobileAds.SetRequestConfiguration(requestConfiguration);
    }

    private void LoadRewardedAd()
    {
        if (!isInitialized || isLoading)
            return;

        isLoading = true;

        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        var adRequest = new AdRequest();
        string adUnitId = GetRewardedAdUnitId();

        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            isLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogWarning($"Rewarded ad failed to load. Error={error}");
                return;
            }

            rewardedAd = ad;
            RegisterRewardedAdEvents(rewardedAd);
            Debug.Log("Rewarded ad loaded.");
        });
    }

    private void RegisterRewardedAdEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad closed.");
            isShowing = false;

            if (hasEarnedReward)
            {
                pendingRewardAction?.Invoke();
            }
            else
            {
                pendingFailedAction?.Invoke();
            }

            hasEarnedReward = false;
            ClearPendingCallbacks();
            LoadRewardedAd();
        };

        ad.OnAdFullScreenContentFailed += error =>
        {
            Debug.LogWarning($"Rewarded ad failed to show. Error={error}");
            isShowing = false;
            hasEarnedReward = false;
            pendingFailedAction?.Invoke();
            ClearPendingCallbacks();
            LoadRewardedAd();
        };
    }

    private string GetRewardedAdUnitId()
    {
        if (useTestAdUnit)
        {
#if UNITY_IOS
            return IosTestRewardedAdUnitId;
#else
            return AndroidTestRewardedAdUnitId;
#endif
        }

#if UNITY_IOS
        return iosRewardedAdUnitId;
#else
        return androidRewardedAdUnitId;
#endif
    }

    private void ClearPendingCallbacks()
    {
        pendingRewardAction = null;
        pendingFailedAction = null;
    }

    private void OnDestroy()
    {
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }
}