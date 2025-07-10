using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{

    public float moveSpeed = 5f;
    public float rotationSpeed = 100f;

    [SerializeField] private InputActionAsset inputActions;

    private InputAction moveAction;
    private InputAction rotateAction;
    void Start()
    {

    }
    void Awake()
    {
        var map = inputActions.FindActionMap("Player");
        moveAction = map.FindAction("Move");
        rotateAction = map.FindAction("Rotate");
    }

    void OnEnable()
    {
        moveAction.Enable();
        rotateAction.Enable();
    }

    void OnDisable()
    {
        moveAction.Disable();
        rotateAction.Disable();
    }
    void Update()
    {
        float move = moveAction.ReadValue<float>() * moveSpeed * Time.deltaTime;
        transform.Translate(Vector3.forward * move);

        float rotate = rotateAction.ReadValue<float>() * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up, rotate);
    }
}
