using UnityEngine;

public class UIToggle : MonoBehaviour
{
    public GameObject uiElement;

    void Update()
    {
        // Seitlicher Trigger vom linken Controller (Grip Button)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            if (uiElement != null)
                uiElement.SetActive(!uiElement.activeSelf);
        }
    }
}