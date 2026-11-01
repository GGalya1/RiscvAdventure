using UnityEngine;

/// <summary>
/// Listens to <see cref="LevelEvents"/> and decides which achievements to unlock.
///
/// Only one observer is ever active (a second one disables itself), so placing it in several
/// scenes can't double-count incremental achievements. <see cref="AchievementManager"/> adds one
/// to its own persistent GameObject automatically, so it also works in level scenes.
/// </summary>
public class AchievementObserver : MonoBehaviour
{
    private static AchievementObserver _active;

    private bool IsPersistent => gameObject.scene.name == "DontDestroyOnLoad";

    private void OnEnable()
    {
        if (_active != null && _active != this)
        {
            // A persistent observer (on the AchievementManager) replaces one that lives in a scene,
            // because the scene one is destroyed on the next scene load.
            if (IsPersistent && !_active.IsPersistent)
            {
                _active.enabled = false; // unsubscribes it via OnDisable
            }
            else
            {
                enabled = false;
                return;
            }
        }
        _active = this;

        LevelEvents.LevelCompleted += OnLevelCompleted;
        LevelEvents.SolutionFailed += OnSolutionFailed;

        LevelEvents.PrevTickUsed += OnPrevTickUsed;
        LevelEvents.CreditsWatched += OnCreditsWatched;
    }

    private void OnDisable()
    {
        if (_active != this) return;
        _active = null;

        LevelEvents.LevelCompleted -= OnLevelCompleted;
        LevelEvents.SolutionFailed -= OnSolutionFailed;

        LevelEvents.PrevTickUsed -= OnPrevTickUsed;
        LevelEvents.CreditsWatched -= OnCreditsWatched;
    }

    private void OnLevelCompleted(LevelCompletedData data)
    {
        // First level ever completed.
        // (Was "SceneIndex == 0", but scene 0 is the main menu, so it could never unlock.)
        AchievementManager.Unlock(AchievementIds.FirstSteps);

        // Three-star solution
        if (data.EarnedStars == 3)
            AchievementManager.Unlock(AchievementIds.PerfectTry);

        // Player beat the last level in the game
        if (data.IsLastLevel)
            AchievementManager.Unlock(AchievementIds.AllLevelsComplete);

        // Fail 5 times, but complete the level
        if (data.FallenTries >= 5)
            AchievementManager.Unlock(AchievementIds.NeverGiveUp);

        // Global "correct solutions" counter (incremental achievement).
        // (Was incremented in OnSolutionFailed, i.e. for WRONG answers.)
        AchievementManager.Increment(AchievementIds.SolutionCounter);
    }

    private void OnSolutionFailed(int totalFailedTries)
    {
        // Nothing for now — "NeverGiveUp" is checked when the level is completed.
    }

    private void OnPrevTickUsed(int totalTicks)
    {
        if (totalTicks >= GameConstants.TargetAchievementTicks)
        {
            AchievementManager.Unlock(AchievementIds.RollingBackFiftyTimes);
        }
    }

    private void OnCreditsWatched()
    {
        AchievementManager.Unlock(AchievementIds.CreditsWatched);
    }
}
