using System;

/*
// Saved level states indexed by tick number, used to step back in time (to previous ticks).
// Index 0 is the state before the first tick.
*/
public sealed class TickHistory<TState> where TState : struct
{
    private readonly TState[] _states;

    public TickHistory(int capacity)
    {
        _states = new TState[Math.Max(capacity, 1)];
    }

    public bool Contains(int tick) => tick >= 0 && tick < _states.Length;

    // Stores the state of tick. ticks outside the capacity are ignored
    public void Save(int tick, TState state)
    {
        if (Contains(tick)) _states[tick] = state;
    }

    public TState this[int tick] => _states[tick];
}