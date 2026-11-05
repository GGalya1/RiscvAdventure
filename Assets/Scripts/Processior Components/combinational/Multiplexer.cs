// Multiplexer with up to three inputs
public static class Multiplexer
{
    public const int NoInput = -1;

    // Output for the given control signal; no selection or an out-of-range selection outputs 0
    public static int Select(int selectedInput, int input0, int input1, int input2 = 0) => selectedInput switch
    {
        0 => input0,
        1 => input1,
        2 => input2,
        _ => 0
    };
}