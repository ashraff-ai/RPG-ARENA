using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance;

    public event EventHandler OnJumpButtonClick;
    public event EventHandler OnAttackButtonClick;

    private PlayerInputAction playerInputAction;

    private bool isJumpHeld;

    private void Awake()
    {
        Instance = this;

        playerInputAction = new PlayerInputAction();

        playerInputAction.Player.Enable();

        playerInputAction.Player.Jump.performed += Jump_performed;
        playerInputAction.Player.Jump.started += Jump_started;
        playerInputAction.Player.Jump.canceled += Jump_canceled;
        playerInputAction.Player.Attack.performed += Attack_performed;

    }

    private void Attack_performed(InputAction.CallbackContext obj)
    {
        OnAttackButtonClick?.Invoke(this, EventArgs.Empty);
    }

    private void Jump_canceled(InputAction.CallbackContext obj)
    {
        isJumpHeld = false;
    }

    private void Jump_started(InputAction.CallbackContext obj)
    {
        isJumpHeld = true;
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
        OnJumpButtonClick?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalize()
    {
        Vector2 moveInput = playerInputAction.Player.Movement.ReadValue<Vector2>();
        moveInput.Normalize();

        return moveInput;
    }

    public bool GetJumpButtonHeld()
    {
        return isJumpHeld;
    }
}
