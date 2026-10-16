using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RiscvAdventure.PlayModeTests
{
    /// <summary>
    /// A "recipe" is a player's solution: for every clock tick, the things the player sets
    /// BEFORE pressing "next tick". Settings persist between ticks exactly as they do in the game.
    ///
    /// Build one with <see cref="For"/>, alternating <see cref="Builder.Before"/> (what to set up)
    /// and <see cref="Builder.Tick"/> (commit that as one tick), then finish with <see cref="Builder.Build"/>:
    /// <code>
    /// Recipe.For("Level 4-0", "put B at address 12")
    ///     .Before(Mux(MuxField, 1), Alu(AluField, Op.Add), WriteEnable(SrcBField, false))
    ///     .Tick()
    ///     .Before(Mux(MuxField, 0), WriteEnable(SrcBField, true))
    ///     .Tick()
    ///     .Build();
    /// </code>
    /// A tick with nothing to set up first is just <c>.Tick()</c> on its own, with no <c>.Before(...)</c>
    /// before it.
    /// </summary>
    public sealed class Recipe
    {
        public string Scene { get; }
        public string Idea { get; }
        public IReadOnlyList<Action<LevelDriver>> Steps { get; }

        /// <summary>Set when the level is known to misbehave; appended to failure messages.</summary>
        public string KnownIssue { get; }

        private Recipe(string scene, string idea, IReadOnlyList<Action<LevelDriver>> steps, string knownIssue)
        {
            Scene = scene;
            Idea = idea;
            Steps = steps;
            KnownIssue = knownIssue;
        }

        /// <summary>Starts building a recipe tick by tick. See the class summary for the pattern.</summary>
        public static Builder For(string scene, string idea) => new(scene, idea);

        public Recipe WithKnownIssue(string issue) => new(Scene, Idea, Steps, issue);

        /// <summary>Same solution, plus one more (empty) tick at the end.</summary>
        public Recipe WithExtraTick() =>
            new(Scene, Idea + " + one extra tick", Steps.Append(null).ToArray(), KnownIssue);

        /// <summary>Same solution, but with an extra action performed before the first tick.</summary>
        public Recipe WithFirstStep(string what, Action<LevelDriver> extra)
        {
            Assert.IsTrue(Steps.Count > 0, $"{Scene}: recipe has no ticks to prepend a step to");

            var steps = Steps.ToArray();
            var original = steps[0];
            steps[0] = d =>
            {
                extra(d);
                original?.Invoke(d);
            };
            return new Recipe(Scene, Idea + " + " + what, steps, KnownIssue);
        }

        public override string ToString() => $"{Scene}: {Idea}";

        /// <summary>Builds a <see cref="Recipe"/> one tick at a time.</summary>
        public sealed class Builder
        {
            private readonly string _scene;
            private readonly string _idea;
            private readonly List<Action<LevelDriver>> _steps = new();
            private Action<LevelDriver>[] _pending = Array.Empty<Action<LevelDriver>>();

            internal Builder(string scene, string idea)
            {
                _scene = scene;
                _idea = idea;
            }

            /// <summary>What to set up before the next <see cref="Tick"/>. Call at most once per tick.</summary>
            public Builder Before(params Action<LevelDriver>[] actions)
            {
                Assert.AreEqual(0, _pending.Length,
                    $"{_scene}: Before() was called twice for the same tick - missing a Tick() in between?");
                _pending = actions;
                return this;
            }

            /// <summary>Commits whatever was passed to <see cref="Before"/> (if anything) as one tick.</summary>
            public Builder Tick()
            {
                _steps.Add(_pending.Length == 0 ? null : Combine(_pending));
                _pending = Array.Empty<Action<LevelDriver>>();
                return this;
            }

            private static Action<LevelDriver> Combine(Action<LevelDriver>[] actions) => d =>
            {
                foreach (var a in actions) a?.Invoke(d);
            };

            public Recipe Build()
            {
                Assert.IsTrue(_steps.Count > 0, $"{_scene}: recipe has no ticks - call Tick() at least once");
                return new Recipe(_scene, _idea, _steps.ToArray(), knownIssue: null);
            }
        }
    }
}
