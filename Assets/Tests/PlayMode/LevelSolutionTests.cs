using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// Plays each level with its intended solution and expects the game to accept it.
    /// </summary>
    public class LevelSolutionTests : LevelTestBase
    {
        public static IEnumerable<TestCaseData> Levels()
        {
            foreach (var recipe in LevelRecipes.Solutions)
            {
                yield return new TestCaseData(recipe).Returns(null).SetName(recipe.Scene);
            }
        }

        [UnityTest]
        [TestCaseSource(nameof(Levels))]
        public IEnumerator IntendedSolution_IsAccepted(Recipe recipe)
        {
            yield return Level.Load(recipe.Scene);
            yield return Level.Play(recipe);

            var result = Level.Submit();

            var issue = recipe.KnownIssue == null ? "" : "\n  KNOWN ISSUE: " + recipe.KnownIssue;
            Assert.IsTrue(result.Accepted,
                Level.Ctx($"the intended solution '{recipe.Idea}' was not accepted; the game said: {result}{issue}"));

            Assert.AreEqual(0, result.FailedTries, Level.Ctx("a first-try solution must not count failed tries"));
            Assert.AreEqual(3, result.Stars, Level.Ctx("a first-try solution must earn 3 stars"));

            // Solving a level unlocks the next scene in the build order.
            Assert.GreaterOrEqual(
                PlayerPrefs.GetInt(GameConstants.UnlockedLevelKey, 1), Level.BuildIndex + 1,
                Level.Ctx("the next level was not unlocked"));
        }
    }
}