/// <summary>
/// Represents a basic digital register used in synchronous logic simulation.
/// <br/>The register stores a value (<see cref="Output"/>) based on the input (<see cref="Input"/>)
/// only if the write-enable signal (<see cref="WriteEnable"/>) is active upon clock cycles.
/// <br/>It implements a two-phase clock logic: PreClockUpdate (Setup) and Clock (Latch).
/// </summary>
public class Register: ISequentialLogic
{
    #region PUBLIC PROPERTIES (I/O and Control)
    /// <summary>
    /// The input value coming from the combinational logic (e.g., ALU or Multiplexer).
    /// This value is potentially saved on the next clock cycle.
    /// </summary>
    public int Input { get; set; }

    /// <summary>
    /// The current output value of the register. This is the stored state
    /// that is provided to other combinatorial blocks.
    /// (Set internally during the <see cref="Clock"/> phase).
    /// </summary>
    public int Output { get; private set; }

    /// <summary>
    /// The Write Enable (WE) control signal.
    /// If <c>true</c>, the current <see cref="Input"/> value will be buffered for saving.
    /// This flag is controlled by external logic (<c>Level_X_Regisseur</c>).
    /// </summary>
    public bool WriteEnable { get; set; }
    #endregion

    #region PRIVATE STATE
    /// <summary>
    /// The buffer value that holds the data to be written to <see cref="Output"/>
    /// on the rising edge of the next clock (during <see cref="Clock"/> call).
    /// </summary>
    private int _nextValue;
    #endregion

    #region CONSTRUCTOR
    /// <summary>
    /// Initializes a new instance of the Register.
    /// </summary>
    /// <param name="initialValue">The starting value for both <see cref="Output"/> and the internal buffer.</param>
    /// <param name="writeEnable">Initial value of the WE signal.</param>
    public Register(int initialValue = 0, bool writeEnable = true)
    {
        Output = initialValue;
        _nextValue = initialValue;
        WriteEnable = writeEnable;
    }
    #endregion

    #region ISEQUENTIAL LOGIC
    /// <summary>
    /// The first clock phase (Setup/Pre-Clock Update).
    /// <br/>This phase checks the <see cref="WriteEnable"/> flag:
    /// <br/>If <c>true</c>, it buffers the current <see cref="Input"/> into <c>_nextValue</c>.
    /// <br/>(Simulates the combinatorial logic preparing the data for the latch).
    /// </summary>
    public void PreClockUpdate()
    {
        _nextValue = WriteEnable ? Input : Output;
    }

    /// <summary>
    /// The second clock phase (Latch/Clock Edge).
    /// <br/>Updates the register's current state (<see cref="Output"/>) with the buffered value (<c>_nextValue</c>).
    /// <br/>(Simulates the data being clocked in on the edge signal).
    /// </summary>
    public void Clock() { 
        Output = _nextValue;

        // so that the actual parameter of WE in next takt not propagate
        //WriteEnable = false;
    }
    #endregion
    
    public void Reset(int value) {
        Output = value;
        _nextValue = value;
    }
}
