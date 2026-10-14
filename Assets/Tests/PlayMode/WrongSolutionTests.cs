using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// The game must NOT accept wrong answers. First case covered here: submitting a level
    /// without touching anything. Almost-right, overshot and frozen-output solutions are
    /// added on top of this in later commits.
    /// Every rejection must be announced (SolutionFailed), count one failed try, award no stars
    /// and leave the next level locked.
    /// </summary>
    public class WrongSolutionTests : LevelTestBase
    {
        // ---- 1. nothing done ------------------------------------------------------------

        public static IEnumerable<TestCaseData> AllLevels()
        {
            foreach (var scene in LevelRecipes.Scenes)
            {
                yield return new TestCaseData(scene).SetName(scene);
            }
        }

        [UnityTest]
        [TestCaseSource(nameof(AllLevels))]
        public IEnumerator UntouchedLevel_IsRejected(string scene)
        {
            yield return Level.Load(scene);

            AssertRejected(Level.Submit(), $"{scene}: submitted without any action");
        }

        // ---- helpers --------------------------------------------------------------------

        private void AssertRejected(SubmitResult result, string what)
        {
            Assert.IsFalse(result.Accepted, Level.Ctx($"WRONG SOLUTION WAS ACCEPTED: {what}"));
            Assert.IsTrue(result.Rejected, Level.Ctx($"no rejection was announced for: {what}"));
            Assert.AreEqual(1, result.FailedTries, Level.Ctx("a rejected solution must count exactly one failed try"));
            Assert.IsFalse(PlayerPrefs.HasKey(GameConstants.UnlockedLevelKey),
                Level.Ctx("a rejected solution must not unlock the next level"));
        }
    }
}
