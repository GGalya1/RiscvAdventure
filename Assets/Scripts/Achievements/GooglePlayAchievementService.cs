// This entire class is compiled ONLY for Android device builds.
#if UNITY_ANDROID && !UNITY_EDITOR

using System;
using System.Collections.Generic;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using UnityEngine;

/// <summary>
/// Google Play Games Services achievement implementation.
///
/// Unlocks/increments that happen while the player is not (yet) signed in — sign-in still running
/// at startup, sign-in failed, no network — are stored in PlayerPrefs and sent after the next
/// successful sign-in instead of being lost.
/// </summary>
public class GooglePlayAchievementService : IAchievementService
{
    private const string PendingUnlocksKey = "Achievements.PendingUnlocks";
    private const string PendingIncrementsKey = "Achievements.PendingIncrements";

    public bool IsAuthenticated =>
        PlayGamesPlatform.Instance != null &&
        PlayGamesPlatform.Instance.IsAuthenticated();

    public void Initialize(Action<bool> onResult = null)
    {
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(status => HandleSignIn(status, onResult));
    }

    private void HandleSignIn(SignInStatus status, Action<bool> onResult)
    {
        var isSuccess = status == SignInStatus.Success;

        CustomLog.LogEditor(isSuccess
            ? "[GPGS] Sign-in successful."
            : $"[GPGS] Sign-in failed. Status: {status}");

        if (isSuccess) FlushPending();

        onResult?.Invoke(isSuccess);
    }

    public void Unlock(string achievementId)
    {
        if (string.IsNullOrEmpty(achievementId)) return;

        if (!IsAuthenticated)
        {
            AddPendingUnlock(achievementId);
            return;
        }

        // ReportProgress to 100% == unlock (also works for incremental achievements)
        PlayGamesPlatform.Instance.ReportProgress(achievementId, 100.0f, success =>
        {
            if (success) return;
            CustomLog.LogEditorError($"[GPGS] Failed to unlock achievement: {achievementId}");
            AddPendingUnlock(achievementId);
        });
    }

    public void Increment(string achievementId, int steps = 1)
    {
        if (string.IsNullOrEmpty(achievementId) || steps <= 0) return;

        if (!IsAuthenticated)
        {
            AddPendingIncrement(achievementId, steps);
            return;
        }

        PlayGamesPlatform.Instance.IncrementAchievement(achievementId, steps, success =>
        {
            if (success) return;
            CustomLog.LogEditorError($"[GPGS] Failed to increment achievement: {achievementId}");
            AddPendingIncrement(achievementId, steps);
        });
    }

    public void ShowUI()
    {
        if (IsAuthenticated)
        {
            PlayGamesPlatform.Instance.ShowAchievementsUI();
            return;
        }

        // Automatic sign-in failed earlier → let the player sign in now instead of doing nothing.
        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            HandleSignIn(status, null);
            if (status == SignInStatus.Success)
                PlayGamesPlatform.Instance.ShowAchievementsUI();
        });
    }

    #region Pending queue (PlayerPrefs)

    private void FlushPending()
    {
        var unlocks = LoadUnlocks();
        var increments = LoadIncrements();

        // Clear first: anything that fails again is re-added by Unlock/Increment.
        PlayerPrefs.DeleteKey(PendingUnlocksKey);
        PlayerPrefs.DeleteKey(PendingIncrementsKey);
        PlayerPrefs.Save();

        foreach (var id in unlocks) Unlock(id);
        foreach (var pair in increments) Increment(pair.Key, pair.Value);
    }

    private static void AddPendingUnlock(string id)
    {
        var unlocks = LoadUnlocks();
        if (!unlocks.Add(id)) return;

        PlayerPrefs.SetString(PendingUnlocksKey, string.Join(";", unlocks));
        PlayerPrefs.Save();
    }

    private static void AddPendingIncrement(string id, int steps)
    {
        var increments = LoadIncrements();
        increments.TryGetValue(id, out var current);
        increments[id] = current + steps;

        var parts = new List<string>(increments.Count);
        foreach (var pair in increments) parts.Add($"{pair.Key}:{pair.Value}");

        PlayerPrefs.SetString(PendingIncrementsKey, string.Join(";", parts));
        PlayerPrefs.Save();
    }

    private static HashSet<string> LoadUnlocks()
    {
        var set = new HashSet<string>();
        var raw = PlayerPrefs.GetString(PendingUnlocksKey, string.Empty);
        foreach (var id in raw.Split(';'))
        {
            if (!string.IsNullOrEmpty(id)) set.Add(id);
        }
        return set;
    }

    private static Dictionary<string, int> LoadIncrements()
    {
        var dict = new Dictionary<string, int>();
        var raw = PlayerPrefs.GetString(PendingIncrementsKey, string.Empty);
        foreach (var entry in raw.Split(';'))
        {
            var sep = entry.LastIndexOf(':');
            if (sep <= 0) continue;
            if (!int.TryParse(entry.Substring(sep + 1), out var steps) || steps <= 0) continue;

            var id = entry.Substring(0, sep);
            dict.TryGetValue(id, out var current);
            dict[id] = current + steps;
        }
        return dict;
    }

    #endregion
}

#endif
