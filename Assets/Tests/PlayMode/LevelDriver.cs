using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace RiscvAdventure.PlayModeTests
{
    public enum Op { Add = 0, Sub = 1, And = 2, Or = 3 }
    
    public enum Imm { I = 0, S = 1, B = 2, J = 3 }
    
    public readonly struct SubmitResult
    {
        public readonly bool Accepted;
        public readonly bool Rejected;
        public readonly int Stars;
        public readonly int FailedTries;

        public SubmitResult(bool accepted, bool rejected, int stars, int failedTries)
        {
            Accepted = accepted;
            Rejected = rejected;
            Stars = stars;
            FailedTries = failedTries;
        }

        public override string ToString() =>
            Accepted ? $"accepted ({Stars} stars, {FailedTries} failed tries)"
            : Rejected ? $"rejected (failed tries: {FailedTries})"
            : "no verdict (neither LevelCompleted nor SolutionFailed was raised)";
    }

    /// <summary>
    /// Plays a level scene the way a player does: it presses the same UI buttons
    /// (MUX path, ALU / Extender operation, write-enable, next tick, check solution)
    /// and observes the game's own <see cref="LevelEvents"/>.
    /// Level internals that are not public (buttons, tick counter) are read through reflection,
    /// so no production code has to change.
    /// </summary>
    public sealed class LevelDriver : IDisposable
    {
        private const float LoadTimeoutSeconds = 30f;
        private const float TickTimeoutSeconds = 60f;

        private readonly List<string> _trace = new();
        private LevelCompletedData? _completed;
        private int? _failedTries;
        private bool _subscribed;

        public string SceneName { get; private set; }
        public int BuildIndex { get; private set; } = -1;
        public MonoBehaviour Regisseur { get; private set; }

        public int TickCounter => Read<int>("TickCounter");
        public int MaxTicks => Read<int>("maxTickNumber");

        // ------------------------------------------------------------------ loading

        public IEnumerator Load(string sceneName)
        {
            SceneName = sceneName;
            BuildIndex = FindBuildIndex(sceneName);

            // Start-up noise (audio mixer, localisation, dialogue) is not what these tests are about.
            LogAssert.ignoreFailingMessages = true;

            // Level scripts read localised strings in Start(); make sure the system is ready first.
            var localisation = LocalizationSettings.InitializationOperation;
            yield return new WaitUntil(() => localisation.IsDone);

            yield return SceneManager.LoadSceneAsync(BuildIndex, LoadSceneMode.Single);

            Regisseur = FindRegisseur();

            // Start() has finished once the tick history exists.
            var deadline = Time.realtimeSinceStartup + LoadTimeoutSeconds;
            while (ReadObject("TickStateValues") == null)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"[{sceneName}] {Regisseur.GetType().Name}.Start() did not complete. " +
                                "Look for an exception in the Console right after the scene loaded.");
                }

                yield return null;
            }

            yield return null;

            LevelEvents.LevelCompleted += OnCompleted;
            LevelEvents.SolutionFailed += OnFailed;
            _subscribed = true;

            LogAssert.ignoreFailingMessages = false;
        }

        private static int FindBuildIndex(string sceneName)
        {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                if (Path.GetFileNameWithoutExtension(path) == sceneName) return i;
            }

            Assert.Fail($"Scene '{sceneName}' is not in the Build Settings.");
            return -1;
        }

        private static MonoBehaviour FindRegisseur()
        {
            var found = new List<MonoBehaviour>();
            foreach (var mb in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (mb != null && IsLevelRegisseur(mb.GetType())) found.Add(mb);
            }

            Assert.AreEqual(1, found.Count, "Expected exactly one BaseLevelRegisseur in the scene.");
            return found[0];
        }

        private static bool IsLevelRegisseur(Type type)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(BaseLevelRegisseur<,>)) return true;
            }

            return false;
        }

        // ------------------------------------------------------------------ player actions
        
        public void SelectMux(string field, int path)
        {
            var mux = Visual<MultiplexerVisualizer>(field);
            var panel = mux.UIController;
            var button = path switch
            {
                0 => panel.FirstWayButton,
                1 => panel.SecondWayButton,
                2 => panel.ThirdWayButton,
                _ => null
            };
            Assert.IsNotNull(button, Ctx($"'{field}' has no MUX input #{path}"));

            button.onClick.Invoke();
            Note($"{field}: MUX path {path}");
            Assert.AreEqual(path, mux.CurrentChosenMuxPath, Ctx($"clicking MUX input #{path} of '{field}' had no effect"));
        }
        
        public void ChooseAlu(string field, Op op) => ClickOperation(field, (int)op, op.ToString().ToUpperInvariant());
        
        public void ChooseImmediate(string field, Imm type) => ClickOperation(field, (int)type, type + "-type");

        private void ClickOperation(string field, int index, string label)
        {
            var vis = Visual<AluVisualiser>(field);
            var panel = vis.uiController;
            var button = index switch
            {
                0 => panel.FirstOperationButton,
                1 => panel.SecondOperationButton,
                2 => panel.ThirdOperationButton,
                3 => panel.FourthOperationButton,
                _ => null
            };
            Assert.IsNotNull(button, Ctx($"'{field}' has no button #{index}"));

            button.onClick.Invoke();
            Note($"{field}: {label}");
            Assert.AreEqual(index, vis.CurrentAluOperation, Ctx($"clicking button #{index} of '{field}' had no effect"));
        }
        
        public void SetWriteEnable(string field, bool enabled)
        {
            var target = ReadObject(field);
            Assert.IsNotNull(target, Ctx($"Field '{field}' is not assigned in the scene"));

            bool Current() => target switch
            {
                RegisterVisualizer r => r.isWriteEnabled,
                InstructionDataMemoryVisualizer m => m.isWriteEnabled,
                RegisterFileVisualizer f => f.isWriteEnabled,
                _ => throw new AssertionException($"'{field}' is a {target.GetType().Name}, which has no write-enable switch")
            };

            Button button = target switch
            {
                RegisterVisualizer r => r.UIRegisterPanel.WeButton,
                InstructionDataMemoryVisualizer m => m.UIRegisterPanel.WeButton,
                RegisterFileVisualizer f => f.UIRegisterPanel.WeButton,
                _ => null
            };

            if (Current() != enabled) button.onClick.Invoke();
            Note($"{field}: write-enable {(enabled ? "ON" : "OFF")}");
            Assert.AreEqual(enabled, Current(), Ctx($"could not switch write-enable of '{field}'"));
        }
        
        public IEnumerator Tick()
        {
            var before = TickCounter;
            Assert.Less(before, MaxTicks, Ctx($"tick limit ({MaxTicks}) already reached"));
            Assert.IsFalse(Read<bool>("isProcessing"), Ctx("previous tick is still running"));

            Read<Button>("nextClick").onClick.Invoke();
            Note($"tick #{before + 1}");

            var deadline = Time.realtimeSinceStartup + TickTimeoutSeconds;
            while (Read<bool>("isProcessing"))
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail(Ctx("tick did not finish (bus animation stuck?)"));
                }

                yield return null;
            }

            Assert.AreEqual(before + 1, TickCounter, Ctx("the tick was not performed"));
        }
        
        public IEnumerator Play(Recipe recipe)
        {
            foreach (var step in recipe.Steps)
            {
                step?.Invoke(this);
                yield return Tick();
            }
        }
        
        public SubmitResult Submit()
        {
            _completed = null;
            _failedTries = null;

            // A rejected solution makes the game log an error on purpose ("Level is not solved!").
            LogAssert.ignoreFailingMessages = true;
            try
            {
                Read<Button>("checkSolutionButton").onClick.Invoke();
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }

            Note("check solution");

            if (_completed.HasValue)
            {
                var c = _completed.Value;
                return new SubmitResult(true, false, c.EarnedStars, c.FallenTries);
            }

            return new SubmitResult(false, _failedTries.HasValue, 0, _failedTries ?? 0);
        }

        // ------------------------------------------------------------------ diagnostics

        /// <summary>Adds the recorded steps and the level state to a failure message.</summary>
        public string Ctx(string message) =>
            $"[{SceneName} / {Regisseur?.GetType().Name}] {message}\n" +
            $"  steps so far: {(_trace.Count == 0 ? "(none)" : string.Join(" -> ", _trace))}\n" +
            $"  level state : {DumpState()}";

        private void Note(string what) => _trace.Add(what);

        private string DumpState()
        {
            try
            {
                var method = FindMethod(Regisseur.GetType(), "GetCurrentState");
                var state = method.Invoke(Regisseur, null);
                var sb = new StringBuilder();
                foreach (var f in state.GetType().GetFields())
                {
                    sb.Append(f.Name).Append('=').Append(f.GetValue(state)).Append("  ");
                }

                return sb.ToString().TrimEnd();
            }
            catch (Exception e)
            {
                return $"(unavailable: {e.GetType().Name})";
            }
        }

        // ------------------------------------------------------------------ reflection helpers

        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static FieldInfo FindField(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var f = t.GetField(name, AnyInstance);
                if (f != null) return f;
            }

            return null;
        }

        private static MethodInfo FindMethod(Type type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
            {
                var m = t.GetMethod(name, AnyInstance);
                if (m != null) return m;
            }

            return null;
        }

        private object ReadObject(string name)
        {
            var field = FindField(Regisseur.GetType(), name);
            Assert.IsNotNull(field,
                $"{Regisseur.GetType().Name} has no field '{name}'. Was it renamed? Update the recipe in LevelRecipes.cs.");
            return field.GetValue(Regisseur);
        }

        private T Read<T>(string name) => (T)ReadObject(name);

        private T Visual<T>(string name) where T : class
        {
            var value = ReadObject(name);
            Assert.IsNotNull(value, Ctx($"Field '{name}' is not assigned in the scene"));
            Assert.IsInstanceOf<T>(value, Ctx($"Field '{name}' is not a {typeof(T).Name}"));
            return (T)value;
        }

        // ------------------------------------------------------------------ events

        private void OnCompleted(LevelCompletedData data) => _completed = data;
        private void OnFailed(int totalFailedTries) => _failedTries = totalFailedTries;

        public void Dispose()
        {
            if (!_subscribed) return;
            LevelEvents.LevelCompleted -= OnCompleted;
            LevelEvents.SolutionFailed -= OnFailed;
            _subscribed = false;
        }
    }
}
