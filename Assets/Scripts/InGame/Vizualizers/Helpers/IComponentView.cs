/// <summary>
/// A visualizer that displays the state of a bound simulation component.
/// The level calls <see cref="Refresh"/> after every clock edge and every history restore.
/// </summary>
public interface IComponentView
{
    void Refresh();
}