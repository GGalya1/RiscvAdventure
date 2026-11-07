using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Serialization;

/// <summary>
/// Base class of every level. Owns the level flow:
/// <br/>- the clock buttons (next/previous tick) and the tick history,
/// <br/>- refreshing and blinking the bound component views,
/// <br/>- checking the solution.
/// <br/>A level only builds its circuit in <see cref="OnLevelStart"/>, binds the views to the
/// components and describes what happens on a clock edge and how the buses are animated.
/// </summary>
public abstract class BaseLevelRegisseur<TState, TBus> : MonoBehaviour
    where TState : struct
    where TBus : class
{
    [FormerlySerializedAs("_levelTargetDescription")]
    [Header("Level Content Configuration")]
    [SerializeField] protected LocalizedString levelTargetDescription;

    [Header("Interactable Components")]
    [SerializeField] private BaseVisualizer[] managedInteractable;

    [FormerlySerializedAs("_correctAnswer")]
    [Tooltip("Correct answer for this level")]
    [SerializeField] protected int correctAnswer;
    protected virtual int RightAnswerValue => correctAnswer;

    // --- CLOCK AND HISTORY CONTROL ---
    [FormerlySerializedAs("_nextClick")]
    [Header("Clock Control")]
    [SerializeField] protected Button nextClick;
    [FormerlySerializedAs("_prevClick")] [SerializeField] protected Button prevClick;
    [FormerlySerializedAs("_currentTickText")] [SerializeField] protected TMP_Text currentTickText;

    [FormerlySerializedAs("_maxTickNumber")]
    [Tooltip("The maximum number of ticks available on level.")]
    [SerializeField] protected int maxTickNumber = 3;

    // --- SOLUTION AND LEVEL MANAGEMENT ---
    [FormerlySerializedAs("_checkSolutionButton")]
    [Header("Solution & Level Management")]
    [SerializeField] protected Button checkSolutionButton;
    [FormerlySerializedAs("_levelManager")] [SerializeField] protected LevelManager levelManager;
    [FormerlySerializedAs("_levelTargetText")] [SerializeField] protected TMP_Text levelTargetText;

    [FormerlySerializedAs("_threeStarCondition")]
    [Header("Star Conditions (Number of failed tries)")]
    [SerializeField] protected int threeStarCondition;
    [FormerlySerializedAs("_twoStarCondition")] [SerializeField] protected int twoStarCondition = 1;
    [FormerlySerializedAs("_oneStarCondition")] [SerializeField] protected int oneStarCondition = 2;

    [FormerlySerializedAs("_crossIndicator")]
    [Header("Feedback / Error Indicator")]
    [SerializeField] protected GameObject crossIndicator;
    [FormerlySerializedAs("_crossDisplayDuration")] [SerializeField] protected float crossDisplayDuration = 0.3f;

    [FormerlySerializedAs("falledTries")] public int fallenTries; // Public for debugging

    [FormerlySerializedAs("_busController")]
    [Header("Bus Visualisation")]
    [SerializeField] protected BusController busController;
    [FormerlySerializedAs("_isProcessing")] public bool isProcessing;

    [Header("Bus Segments")]
    public TBus buses;

    // --- STATE ---
    /// <summary>
    /// Number of completed ticks. While a tick is executed (bus animation and <see cref="HandleClockUpdate"/>)
    /// it is the index of that tick; while a tick is undone (<see cref="ReverseBusVisualizations"/>)
    /// it is the index of the tick being undone.
    /// </summary>
    protected int TickCounter;

    /// <summary> All sequential components of the level. <see cref="HandleClockUpdate"/> ends with <c>Clock.Tick()</c>. </summary>
    protected readonly ClockDomain Clock = new();

    protected BusController Bus => busController;

    private readonly List<IComponentView> _views = new();
    private TickHistory<TState> _history;

    #region LEVEL DEFINITION (implemented by every level)
    /// <summary> Create the components, add the sequential ones to <see cref="Clock"/> and bind the views (<see cref="Show"/>). </summary>
    protected virtual void OnLevelStart() { }

    /// <summary> One clock edge: drive the inputs of the sequential components, then call <c>Clock.Tick()</c>. </summary>
    protected abstract void HandleClockUpdate();

    protected abstract TState GetCurrentState();

    /// <summary> Restores a saved state into the components (bound views update themselves / on refresh). </summary>
    protected abstract void ApplyState(TState state);

    /// <summary> Bus animation of the tick that is about to be executed (state before the clock edge). </summary>
    protected abstract IEnumerator RunBusVisualizations();

    /// <summary> Bus animation of the tick that is being undone (state still after the clock edge). </summary>
    protected abstract IEnumerator ReverseBusVisualizations();

    protected abstract bool CheckWinCondition();
    #endregion

    #region VIEWS
    /// <summary> Registers views that display component state; they are refreshed after every tick and undo. </summary>
    protected void Show(params IComponentView[] views)
    {
        foreach (var view in views)
        {
            if (view != null && !_views.Contains(view)) _views.Add(view);
        }
    }

    /// <summary> Default: every shown view whose component is clocked blinks on the clock edge. </summary>
    protected virtual void BlinkClockedComponents()
    {
        foreach (var view in _views)
        {
            if (view is SequentialVisualizer sequential && Clock.Contains(sequential.Model))
                sequential.Blink();
        }
    }

    /// <summary> Hook for displays that are not component views (e.g. the side panel). </summary>
    protected virtual void OnViewsRefreshed() { }

    private void RefreshViews()
    {
        foreach (var view in _views) view.Refresh();
        OnViewsRefreshed();
    }

    /// <summary> Saved state of the given tick (0 = before the first tick). </summary>
    protected TState StateAt(int tick) => _history[tick];
    #endregion

    // --- UNITY LIFECYCLE & CORE LOGIC ---

    protected virtual void Start()
    {
        OnLevelStart();
        RefreshViews();

        nextClick.onClick.AddListener(HandleNextTick);
        prevClick.onClick.AddListener(HandlePrevTick);
        checkSolutionButton.onClick.AddListener(CheckSolution);
        currentTickText.SetText("{0}", TickCounter);
        levelTargetText.text = levelTargetDescription.GetLocalizedString();

        _history = new TickHistory<TState>(maxTickNumber);
        _history.Save(0, GetCurrentState());
    }

    #region CLOCK
    private void HandleNextTick()
    {
        if (TickCounter >= maxTickNumber || isProcessing) return;
        StartCoroutine(NextTickSequence());
    }

    private IEnumerator NextTickSequence()
    {
        SetProcessing(true);

        BlinkClockedComponents();
        yield return StartCoroutine(RunBusVisualizations());

        HandleClockUpdate();

        TickCounter++;
        currentTickText.SetText("{0}", TickCounter);
        RefreshViews();
        _history.Save(TickCounter, GetCurrentState());

        SetProcessing(false);
    }

    private void HandlePrevTick()
    {
        if (TickCounter <= 0 || isProcessing) return;

        AchievementManager.Increment(AchievementIds.RollingBackFiftyTimes);

        StartCoroutine(PrevTickSequence());
    }

    private IEnumerator PrevTickSequence()
    {
        SetProcessing(true);

        TickCounter--;
        currentTickText.SetText("{0}", TickCounter);

        yield return StartCoroutine(ReverseBusVisualizations());

        ApplyState(_history[TickCounter]);
        RefreshViews();

        SetProcessing(false);
    }

    private void SetProcessing(bool value)
    {
        isProcessing = value;
        nextClick.interactable = !value;
        prevClick.interactable = !value;
        foreach (var v in managedInteractable) v.SetInteractable(!value);
    }
    #endregion

    #region SOLUTION
    protected virtual void CheckSolution()
    {
        if (CheckWinCondition())
        {
            CustomLog.LogEditor("Level is solved!");

            var earnedStars = new StarRating(threeStarCondition, twoStarCondition, oneStarCondition).StarsFor(fallenTries);
            LevelProgress.CompleteCurrentLevel(fallenTries, earnedStars);

            levelManager.SetGainedStars(earnedStars);
            levelManager.OpenEndOfLevelMenu();
        }
        else
        {
            fallenTries++;
            CustomLog.LogEditorError("Level is not solved! Failed tries: " + fallenTries);

            LevelProgress.ReportFailedTry(fallenTries);
            StartCoroutine(new WrongAnswerFeedback(crossIndicator, crossDisplayDuration).Play());
        }
    }
    #endregion
}
