using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;

public class MovePlayer : MonoBehaviour
{

    public float moveSpeed = 0.2f;
    public float rotationSpeed = 100f;

    [SerializeField] private InputActionAsset inputActions;


    private InputAction goUpAction;
    private InputAction goDownAction;
    void Start()
    {

    }
    void Awake()
    {
        var map = inputActions.FindActionMap("PlayerMove");
        goUpAction = map.FindAction("MoveUp");
        goDownAction = map.FindAction("MoveDown");
    }

    void OnEnable()
    {

        goUpAction.Enable();
        goDownAction.Enable();
    }

    void OnDisable()
    {

        goUpAction.Disable();
        goDownAction.Disable();
    }
    void Update()
    {
        Vector3 moveDirection = Vector3.zero;

        if (goUpAction.IsPressed())
        {
            moveDirection += Vector3.up;
        }

        if (goDownAction.IsPressed())
        {
            moveDirection -= Vector3.up;
        }

        if (moveDirection != Vector3.zero)
        {
            transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);
        }
    }
}
