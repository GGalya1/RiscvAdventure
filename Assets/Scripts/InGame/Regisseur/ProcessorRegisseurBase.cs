/*
 Common part of the full processor levels (single-cycle and multi-cycle): the level data (Initial) 
 is chosen by the level loader, and the next sub-level is offered to the LevelManager when the current
 one is finished.
*/
public abstract class ProcessorRegisseurBase<TState, TBus, TInitial> : BaseLevelRegisseur<TState, TBus>
    where TState : struct
    where TBus : IBusSegmentProvider
    where TInitial : BaseProcessorInitialState
{
    // Data of the current sub-level. Set by the level loader before the scene is loaded
    public static TInitial Initial;

    protected virtual void Awake()
    {
        levelManager.SetLevelDialogue(Initial.customDialogueGraph);
    }

    protected override void OnLevelStart()
    {
        if (Initial != null) levelTargetDescription = Initial.levelTarget;
    }

    #region transition to next level
    protected virtual void OnEnable()
    {
        LevelManager.OnRequestNextLevelData += GetNextLevelData;
    }

    protected virtual void OnDisable()
    {
        LevelManager.OnRequestNextLevelData -= GetNextLevelData;
    }

    private static object GetNextLevelData()
    {
        if (Initial != null && Initial.nextSceneInitial != null) return Initial.nextSceneInitial;
        return null;
    }
    #endregion
}