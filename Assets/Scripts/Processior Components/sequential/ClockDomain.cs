using System.Collections.Generic;

/*
 All sequential elements that share one clock. `Tick` performs one clock edge:
 first every element samples its inputs, then every element latches.
*/
public sealed class ClockDomain
{
    private readonly List<ISequentialLogic> _elements = new();

    public void Add(params ISequentialLogic[] elements)
    {
        foreach (var element in elements)
        {
            if (element != null && !_elements.Contains(element))
                _elements.Add(element);
        }
    }

    public bool Contains(ISequentialLogic element) => element != null && _elements.Contains(element);

    public void Tick()
    {
        foreach (var element in _elements) element.PreClockUpdate();
        foreach (var element in _elements) element.Clock();
    }
}