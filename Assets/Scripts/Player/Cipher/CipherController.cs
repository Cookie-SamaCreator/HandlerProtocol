using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Stamina))]
public class CipherController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    [Header("Look")]
    public Transform cameraHolder;
    public float mouseSensitivity = 1.0f;
    public float pitchMin = -85f, pitchMax = 85f;

    [Space]
    public Weapon currentWeapon;

    private CharacterController cc;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private float pitch = 0f;
    private bool isSprinting;
    private bool firePressed;
    private Stamina stamina;



    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        stamina = GetComponent<Stamina>();
    }

    private void Update()
    {
        HandleLook();
        HandleMovement();
        HandleFire();
        if (stamina != null)
        {
            Debug.Log($"Stamina: {stamina.currentStamina:0.0}/{stamina.maxStamina}");
        }
    }

    #region Input
    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        lookInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && cc.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(-2f * gravity * jumpHeight);
        }
    }

    public void OnFire(InputValue value) => firePressed = value.isPressed;

    public void OnSprint(InputValue value)
    {
        isSprinting = value.isPressed;
    }

    #endregion

    private void HandleLook()
    {
        Vector2 look = lookInput * mouseSensitivity;
        float yaw = look.x;
        float pitchDelta = -look.y;

        transform.Rotate(Vector3.up, yaw, Space.Self);

        pitch += pitchDelta;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
        if (cameraHolder) cameraHolder.localEulerAngles = Vector3.right * pitch;
    }

    private void HandleMovement()
    {
        Vector3 input = new(moveInput.x, 0f, moveInput.y);
        Vector3 worldMove = transform.TransformDirection(input);

        bool canSprint = isSprinting && stamina != null && stamina.HasStamina();
        float targetSpeed = canSprint ? sprintSpeed : walkSpeed;

        if (isSprinting && stamina != null)
        {
            if (canSprint)
                stamina.StartDraining();
            else
                stamina.StopDraining();
        }
        else if (stamina != null)
        {
            stamina.StopDraining();
        }

        if (cc.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        Vector3 velocity = worldMove * targetSpeed + Vector3.up * verticalVelocity;
        cc.Move(velocity * Time.deltaTime);
    }

    private void HandleFire()
    {
        if (currentWeapon == null) return;

        // Si bouton maintenu
        if (firePressed)
        {
            currentWeapon.TryFire();
        }
    }
}
