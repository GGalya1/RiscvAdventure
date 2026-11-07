using UnityEngine;
using UnityEngine.EventSystems;

/// <summary> Forwards a click on the 3D object to its <see cref="IVisualizer"/> (opens/closes the info panel). </summary>
public class ClickableObject : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (TryGetComponent<IVisualizer>(out var visualizer))
        {
            visualizer.ShowData();
        }
    }
}