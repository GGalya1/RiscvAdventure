using System;

/// <summary>
/// Multiplexer with up to three inputs and one control signal (<see cref="SelectedInput"/>).
/// <br/>An unselected multiplexer (<see cref="NoInput"/>) or an out-of-range selection outputs 0.
/// </summary>
public sealed class Multiplexer
{
    public const int NoInput = -1;

    private int _selectedInput = NoInput;

    // Raised every time the control signal is assigned
    public event Action<int> SelectionChanged;

    public int SelectedInput
    {
        get => _selectedInput;
        set
        {
            _selectedInput = value;
            SelectionChanged?.Invoke(value);
        }
    }

    public bool HasSelection => _selectedInput != NoInput;

    // Output for the current control signal
    public int Route(int input0, int input1, int input2 = 0) => Select(_selectedInput, input0, input1, input2);

    // Output for an arbitrary (e.g. saved) control signal
    public static int Select(int selectedInput, int input0, int input1, int input2 = 0) => selectedInput switch
    {
        0 => input0,
        1 => input1,
        2 => input2,
        _ => 0
    };
}