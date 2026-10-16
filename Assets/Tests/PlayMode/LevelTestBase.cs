using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// Shared set-up: fast animations, a clean "unlocked level" save slot that is restored afterwards.
    /// </summary>
    public abstract class LevelTestBase
    {
        protected LevelDriver Level;

        private bool _hadUnlockKey;
        private int _unlockedBefore;

        [UnitySetUp]
        public IEnumerator BaseSetUp()
        {
            // The game stores progress in PlayerPrefs. Keep the developer's own progress intact.
            _hadUnlockKey = PlayerPrefs.HasKey(GameConstants.UnlockedLevelKey);
            _unlockedBefore = PlayerPrefs.GetInt(GameConstants.UnlockedLevelKey, 1);
            PlayerPrefs.DeleteKey(GameConstants.UnlockedLevelKey);

            // Bus signals travel at a fixed speed; run the clock faster so 19 levels finish quickly.
            Time.timeScale = 8f;

            Level = new LevelDriver();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator BaseTearDown()
        {
            Level?.Dispose();
            LogAssert.ignoreFailingMessages = false;
            Time.timeScale = 1f;

            if (_hadUnlockKey) PlayerPrefs.SetInt(GameConstants.UnlockedLevelKey, _unlockedBefore);
            else PlayerPrefs.DeleteKey(GameConstants.UnlockedLevelKey);
            PlayerPrefs.Save();

            yield return null;
        }
    }
}
