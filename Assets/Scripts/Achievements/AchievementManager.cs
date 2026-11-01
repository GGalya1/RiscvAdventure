using UnityEngine;

/*
 Singleton that owns and exposes the active platform achievement service.
*/
public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance { get; private set; }

    private IAchievementService _service;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // The observer has to survive scene loads too, otherwise nothing is unlocked inside levels.
        if (!TryGetComponent<AchievementObserver>(out _))
            gameObject.AddComponent<AchievementObserver>();

        _service = CreateService();
        _service.Initialize(OnAuthResult);
    }

    /// <summary>
    /// Compile-time platform switch (currently developing only for Google Play).
    /// </summary>
    private static IAchievementService CreateService()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return new GooglePlayAchievementService();
#else
        return new NullAchievementService();
#endif
    }

    private static void OnAuthResult(bool success)
    {
        CustomLog.LogEditor(success
            ? "[Achievements] Authenticated."
            : "[Achievements] Not authenticated (editor / non-Android / sign-in failed).");
    }

    /// <summary>Unlocks a standard achievement. Safe on every platform.</summary>
    public static void Unlock(string achievementId)
    {
        if (Instance == null)
        {
            CustomLog.LogEditorWarning("[Achievements] AchievementManager missing from scene!");
            return;
        }
        Instance._service.Unlock(achievementId);
    }

    /// <summary>Increments an incremental achievement. Safe on every platform.</summary>
    public static void Increment(string achievementId, int steps = 1)
    {
        if (Instance == null)
        {
            CustomLog.LogEditorWarning("[Achievements] AchievementManager missing from scene!");
            return;
        }
        Instance._service.Increment(achievementId, steps);
    }

    /// <summary>Opens the platform achievements overlay (Android only, no-op elsewhere).</summary>
    public static void ShowAchievementsUI() => Instance?._service.ShowUI();
}
