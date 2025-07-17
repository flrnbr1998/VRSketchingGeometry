using UnityEngine;
using UnityEngine.InputSystem;

public class UIToggle : MonoBehaviour
{
    public GameObject uiElement;
    public GameObject keyboard;

    void Update()
    {
        // Seitlicher Trigger vom linken Controller (Grip Button)
        if (OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch))
        {
            if (uiElement != null)
                uiElement.SetActive(!uiElement.activeSelf);
                keyboard.SetActive(false);
        }
    }
}