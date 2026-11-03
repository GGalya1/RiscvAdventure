using UnityEngine;

// One value sent along one bus segment
public readonly struct BusPulse
{
    public readonly LineRenderer Segment;
    public readonly int Value;

    public BusPulse(LineRenderer segment, int value)
    {
        Segment = segment;
        Value = value;
    }

    public static implicit operator BusPulse((LineRenderer segment, int value) pulse) =>
        new(pulse.segment, pulse.value);
}