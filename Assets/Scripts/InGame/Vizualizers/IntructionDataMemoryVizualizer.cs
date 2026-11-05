/// <summary> View of a <see cref="DataInstMemory"/>: shows the words at 0, 4, 8, 12, the WE state and blinks on the clock edge. </summary>
public class InstructionDataMemoryVisualizer : SequentialVisualizer
{
    private InstrMemoryControlPanel _panel;
    private DataInstMemory _memory;
    private MemoryDisplay _display;

    protected override void InitializePanel()
    {
        _panel = RequirePanel<InstrMemoryControlPanel>();
    }

    public InstructionDataMemoryVisualizer Bind(DataInstMemory memory, MemoryDisplay display)
    {
        _memory = memory;
        _display = display;
        BindModel(memory);
        return this;
    }

    protected override void RefreshContent()
    {
        var w0 = _memory.Read(0);
        var w1 = _memory.Read(4);
        var w2 = _memory.Read(8);
        var w3 = _memory.Read(12);

        if (_display == MemoryDisplay.Values)
        {
            _panel.Display(w0, w1, w2, w3);
        }
        else
        {
            _panel.Display(
                RiscVDecoder.CommandBuilder((uint)w0),
                RiscVDecoder.CommandBuilder((uint)w1),
                RiscVDecoder.CommandBuilder((uint)w2),
                RiscVDecoder.CommandBuilder((uint)w3));
        }
    }
}