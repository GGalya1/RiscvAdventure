/// <summary> A clickable 3D component that opens/closes its info panel. Used by <see cref="ClickableObject"/>. </summary>
public interface IVisualizer
{
    /// <summary> Opens the panel, or closes it if it is already open. </summary>
    void ShowData();

    void HideData();
}