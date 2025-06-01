using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{

    public float moveSpeed = 5f;         // Geschwindigkeit vor/zurück
    public float rotationSpeed = 100f;   // Drehgeschwindigkeit links/rechts
    // Start is called before the first frame update
    void Start()
    {
      
    }

    // Update is called once per frame
    void Update()
    {
        // Vorwärts/Rückwärts bewegen
        float move = Input.GetAxis("Vertical") * moveSpeed * Time.deltaTime;
        transform.Translate(Vector3.forward * move);

        // Links/Rechts drehen
        float rotate = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotate);
    }
}
