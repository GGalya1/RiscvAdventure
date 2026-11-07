public interface ISequentialLogic
{
    // When false, the element keeps its state on the clock edge
    bool WriteEnable { get; set; }

    void PreClockUpdate();

    void Clock();
}