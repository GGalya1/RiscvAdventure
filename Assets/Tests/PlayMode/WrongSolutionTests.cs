using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// The game must NOT accept wrong answers. Four kinds of wrong solution:
    /// <list type="number">
    /// <item>submitting a level without touching anything,</item>
    /// <item>"almost right" solutions with one wrong choice,</item>
    /// <item>the correct solution, but the player keeps ticking past the correct moment,</item>
    /// <item>the correct solution, but the result register cannot be written (write-enable OFF).</item>
    /// </list>
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
                yield return new TestCaseData(scene).Returns(null).SetName(scene);
            }
        }

        [UnityTest]
        [TestCaseSource(nameof(AllLevels))]
        public IEnumerator UntouchedLevel_IsRejected(string scene)
        {
            yield return Level.Load(scene);

            AssertRejected(Level.Submit(), $"{scene}: submitted without any action");
        }

        // ---- 2. almost right ------------------------------------------------------------

        public static IEnumerable<TestCaseData> NearMisses() => ToCases(LevelRecipes.NearMisses);

        [UnityTest]
        [TestCaseSource(nameof(NearMisses))]
        public IEnumerator AlmostCorrectSolution_IsRejected(Recipe recipe)
        {
            yield return Level.Load(recipe.Scene);
            yield return Level.Play(recipe);

            AssertRejected(Level.Submit(), recipe.ToString());
        }

        // ---- 3. one tick too many -------------------------------------------------------

        public static IEnumerable<TestCaseData> Overshoots() => ToCases(LevelRecipes.Overshoots);

        [UnityTest]
        [TestCaseSource(nameof(Overshoots))]
        public IEnumerator CorrectSolutionRunOneTickTooLong_IsRejected(Recipe recipe)
        {
            yield return Level.Load(recipe.Scene);
            yield return Level.Play(recipe);

            AssertRejected(Level.Submit(), recipe.ToString());
        }

        // ---- 4. result register frozen --------------------------------------------------

        public static IEnumerable<TestCaseData> FrozenOutputs() => ToCases(LevelRecipes.OutputWriteDisabled);

        [UnityTest]
        [TestCaseSource(nameof(FrozenOutputs))]
        public IEnumerator CorrectSolutionWithOutputWriteDisabled_IsRejected(Recipe recipe)
        {
            yield return Level.Load(recipe.Scene);
            yield return Level.Play(recipe);

            AssertRejected(Level.Submit(), recipe.ToString());
        }

        // ---- helpers --------------------------------------------------------------------

        private static IEnumerable<TestCaseData> ToCases(IEnumerable<Recipe> recipes)
        {
            foreach (var recipe in recipes)
            {
                yield return new TestCaseData(recipe).Returns(null).SetName(recipe.Scene);
            }
        }

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