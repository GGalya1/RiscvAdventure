using UnityEngine;
using UnityEngine.EventSystems;

public class ClickableObject: MonoBehaviour, IPointerClickHandler
{
    // this method will call when we click on a object
    public void OnPointerClick(PointerEventData eventData)
    {
        // releasing the finger/mouse at the end of a camera drag is not a click
        if (CameraController.IsDragInProgressOrJustEnded) return;

        // get the IVisualizer from an object
        var visualiser = GetComponent<IVisualizer>();

        if (visualiser != null)
        {
            // call the functionality of the object, if it have component vizualizer
            visualiser.ShowData();
        }
    }
}