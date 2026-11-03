using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Number of failed tries allowed for 3, 2 and 1 stars
public readonly struct StarRating
{
    private readonly int _threeStarMaxTries;
    private readonly int _twoStarMaxTries;
    private readonly int _oneStarMaxTries;

    public StarRating(int threeStarMaxTries, int twoStarMaxTries, int oneStarMaxTries)
    {
        _threeStarMaxTries = threeStarMaxTries;
        _twoStarMaxTries = twoStarMaxTries;
        _oneStarMaxTries = oneStarMaxTries;
    }

    public int StarsFor(int failedTries)
    {
        if (failedTries <= _threeStarMaxTries) return 3;
        if (failedTries <= _twoStarMaxTries) return 2;
        if (failedTries <= _oneStarMaxTries) return 1;
        return 0;
    }
}

// Saves the unlocked levels and notifies the achievement system about results
public static class LevelProgress
{
    // Unlocks the scene after the current one and reports the completion
    public static void CompleteCurrentLevel(int failedTries, int earnedStars)
    {
        var currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        var nextSceneIndex = currentSceneIndex + 1;
        var highestUnlockedIndex = PlayerPrefs.GetInt(GameConstants.UnlockedLevelKey, 1);

        if (nextSceneIndex > highestUnlockedIndex)
        {
            PlayerPrefs.SetInt(GameConstants.UnlockedLevelKey, nextSceneIndex);
            PlayerPrefs.Save();
            CustomLog.LogEditor($"New level unlocked: Scene Index {nextSceneIndex}");
        }

        var isLastLevel = nextSceneIndex >= SceneManager.sceneCountInBuildSettings;
        Notify(() => LevelEvents.RaiseLevelCompleted(
            new LevelCompletedData(currentSceneIndex, failedTries, earnedStars, isLastLevel)));
    }

    public static void ReportFailedTry(int failedTries)
    {
        Notify(() => LevelEvents.RaiseSolutionFailed(failedTries));
    }

    // An exception in an achievement observer must never break the level flow.
    private static void Notify(Action raise)
    {
        try
        {
            raise();
        }
        catch (Exception e)
        {
            Debug.LogError($"Achievement observer cannot be notified: {e}");
        }
    }
}
