// Visualizer of an instruction/data memory: words at 0, 4, 8, 12, WE state, blinking on the clock edge
public class InstructionDataMemoryVisualizer : SequentialVisualizer
{
    public InstrMemoryControlPanel UIRegisterPanel { get; private set; }

    protected override void InitializePanel()
    {
        UIRegisterPanel = RequirePanel<InstrMemoryControlPanel>();
    }
}