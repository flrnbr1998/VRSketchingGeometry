using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardPosition : MonoBehaviour
{

    [SerializeField] private float offsetFront = 0.2f;

    [SerializeField] private float offsetDown = -0.2f;

    [SerializeField] private float neigung = -10.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame


    void Update()
    {
        if (Camera.main == null) return;

        Transform cam = Camera.main.transform;

        // Position vor der Kamera mit leichtem Offset nach unten
        transform.position = cam.position + cam.forward * offsetFront - cam.up * offsetDown;

        // Rotation direkt auf Kamera ausrichten (optional leicht nach unten neigen)
        transform.rotation = Quaternion.LookRotation(cam.forward, cam.up) * Quaternion.Euler(neigung, 0, 0); ;
    }
    
}
