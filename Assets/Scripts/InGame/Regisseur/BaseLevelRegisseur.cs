using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Localization;
using UnityEngine.Serialization;

// A base class for all levels that manages time, history, and completion.
public abstract class BaseLevelRegisseur<TState, TBus> : MonoBehaviour 
    where TState: struct
    where TBus: IBusSegmentProvider
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

    // Requirements for earning stars
    [FormerlySerializedAs("_threeStarCondition")]
    [Header("Star Conditions (Number of failed tries)")]
    [SerializeField] protected int threeStarCondition;
    [FormerlySerializedAs("_twoStarCondition")] [SerializeField] protected int twoStarCondition = 1;
    [FormerlySerializedAs("_oneStarCondition")] [SerializeField] protected int oneStarCondition = 2;

    [FormerlySerializedAs("_crossIndicator")]
    [Header("Feedback / Error Indicator")]
    [SerializeField] protected GameObject crossIndicator;
    [FormerlySerializedAs("_crossDisplayDuration")] [SerializeField] protected float crossDisplayDuration = 0.3f;

    
    // --- STATE ---
    protected int TickCounter;
    private TickHistory<TState> _history;
    [FormerlySerializedAs("falledTries")] public int fallenTries; // Public for debugging

    [FormerlySerializedAs("_busController")]
    [Header("Bus Visualisation")]
    [SerializeField] protected BusController busController;
    [FormerlySerializedAs("_isProcessing")] public bool isProcessing;
    
    // --- SIGNAL ANIMATION
    protected WaitUntil WaitNoSignals;
    
    [Header("Bus Segments")]
    public TBus buses;

    protected BusController Bus => busController;


    #region ABSTRACT METHODS (Unique to each level)
    /// <summary> The main method that implements the logic of a single clock cycle (PreClock/Clock). </summary>
    protected abstract void HandleClockUpdate();

    /// <summary> Must return the current state in a format unique to the level (e.g., levelOneState). </summary>
    protected abstract TState GetCurrentState();

    /// <summary> Applies the saved state to the level's internal components. </summary>
    protected abstract void ApplyState(TState state);

    /// <summary> Visually indicates when a tick is triggered (e.g., flashing registers). </summary>
    protected abstract void BlinkClockedComponents();
    #endregion

    /// <summary> 
    /// New abstract method: Runs a bus visualization specific to the current cycle.
    /// Must be implemented in a subclass.
    /// </summary>
    protected abstract IEnumerator RunBusVisualizations();

    /// <summary> 
    /// New abstract method: Stops and destroys all active signals 
    /// (or resets them) when transitioning to the previous bar.
    /// Must be implemented in a subclass.
    /// </summary>
    protected abstract IEnumerator ReverseBusVisualizations();

    // --- UNITY LIFECYCLE & CORE LOGIC ---

    protected virtual void Start()
    {
        OnLevelStart();

        // 1. Initializing the UI and subscriptions
        nextClick.onClick.AddListener(HandleNextTick);
        prevClick.onClick.AddListener(HandlePrevTick);
        checkSolutionButton.onClick.AddListener(CheckSolution);
        currentTickText.SetText("{0}", TickCounter);
        levelTargetText.text = levelTargetDescription.GetLocalizedString();

        // 2. Initializing history
        _history = new TickHistory<TState>(maxTickNumber);
        _history.Save(0, GetCurrentState());
        
        buses.RegisterAll(busController);
        WaitNoSignals = new WaitUntil(() => busController.NoActiveSignals);
    }

    protected virtual void OnLevelStart()
    {
        // A method for overriding in a subclass (e.g., setting initial values for registers or strings)
    }

    private void HandleNextTick()
    {
        if (TickCounter >= maxTickNumber || isProcessing) { return; }
        StartCoroutine(NextTickSequence());
    }
    private IEnumerator NextTickSequence()
    {
        isProcessing = true;
        nextClick.interactable = false; // Visually disable the buttons
        prevClick.interactable = false;
        BlockInGameInteractable();

        BlinkClockedComponents();

        // We are waiting for the entire rendering to complete in the child class
        yield return StartCoroutine(RunBusVisualizations());

        HandleClockUpdate();

        TickCounter++;
        currentTickText.SetText("{0}", TickCounter);
        UpdateVisualizers();

        _history.Save(TickCounter, GetCurrentState());

        isProcessing = false;
        nextClick.interactable = true;
        prevClick.interactable = true;
        ReleaseInGameInteractable();
    }

    private void HandlePrevTick()
    {
        if (TickCounter <= 0 || isProcessing) { return; }
        
        AchievementManager.Increment(AchievementIds.RollingBackFiftyTimes);
        
        StartCoroutine(PrevTickSequence());
    }
    private IEnumerator PrevTickSequence()
    {
        isProcessing = true;
        nextClick.interactable = false;
        prevClick.interactable = false;
        BlockInGameInteractable();

        TickCounter--;
        currentTickText.SetText("{0}", TickCounter);

        // We are waiting for the reverse rendering to finish
        yield return StartCoroutine(ReverseBusVisualizations());

        ApplyState(_history[TickCounter]);
        UpdateVisualizers();

        isProcessing = false;
        nextClick.interactable = true;
        prevClick.interactable = true;
        ReleaseInGameInteractable();
    }

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
    protected abstract bool CheckWinCondition();
    
    private void BlockInGameInteractable() {
        foreach (var v in managedInteractable) v.SetInteractable(false);
    }
    private void ReleaseInGameInteractable() {
        foreach (var v in managedInteractable) v.SetInteractable(true);
    }

    #region HISTORY METHODS
    // Saved state of the given tick (0 = before the first tick)
    protected TState StateAt(int tick) => _history[tick];

    protected abstract void UpdateVisualizers();
    #endregion
    
    # region STATIC HELPER METHODS
    
    protected static void ApplyMuxState(int path, MultiplexerVisualizer mux)
    {
        switch (path)
        {
            case -1:
                mux.ResetVisualisation();
                break;
            case >= 0 and <= 2:
                mux.SelectPath(path);
                break;
        }
    }
    
    protected static int EvaluateMux(int path, int v0, int v1, int v2) => path switch
    {
        0 => v0, 1 => v1, 2 => v2,
        _ => 0
    };
    
    protected IEnumerator DelayedSignal(LineRenderer seg, int value, bool reverse = false)
    {
        yield return WaitNoSignals;
        busController.StartBusSignal(seg, value, reverse);
    }

    protected IEnumerator DelayedSignals(
        LineRenderer seg1, int val1,
        LineRenderer seg2, int val2,
        bool rev1 = false, bool rev2 = false)
    {
        yield return WaitNoSignals;
        busController.StartBusSignal(seg1, val1, rev1);
        busController.StartBusSignal(seg2, val2, rev2);
    }
    # endregion
}