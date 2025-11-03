using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(Health))]
public class CipherController : MonoBehaviour
{
    [Header("Character Controller")]
    [SerializeField] private CharacterController cc;

    [Header("Movement")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.2f;

    [Header("Look")]
    public Transform cameraHolder;
    public float sensitivityX = 1.2f;
    public float sensitivityY = 1.0f;
    public float minPitch = -85f;
    public float maxPitch = 85f;
    private float cameraPitch = 0f;

    [Space]
    public Weapon currentWeapon;

    [Header("Network Simulation")]
    public bool isLocalPlayer = false; // IsOwner

    public bool IsMoving => moveInput.magnitude > 0.1f;
    public bool IsSprinting => isSprinting && (stamina == null || stamina.HasStamina());
    public bool StaminaAvailable() => stamina == null || stamina.HasStamina();
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private bool isSprinting;
    private bool firePressed;
    public Stamina stamina;
    public Health health;
    private PlayerControls controls;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        controls = new PlayerControls();
        controls.Cipher.Enable();
    }

    private void OnEnable()
    {
        controls.Cipher.Enable();
    }
    private void OnDisable()
    {
        controls.Cipher.Disable();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        HandleLook();
        HandleMovement();
        HandleFire();
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

    #endregion

    private void HandleLook()
    {    
        float lookX = lookInput.x * sensitivityX;
        float lookY = lookInput.y * sensitivityY;
    
        transform.Rotate(Vector3.up * lookX, Space.Self);
    
        cameraPitch -= lookY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
    
        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }


    private void HandleMovement()
    {
        Vector3 input = new(moveInput.x, 0f, moveInput.y);
        Vector3 worldMove = transform.TransformDirection(input);

        bool sprintHeld = controls.Cipher.Sprint.ReadValue<float>() > 0.5f;
        isSprinting = sprintHeld;

        bool isMovingForward = moveInput.magnitude > 0.1f || moveInput.y > 0.1f ;
        bool canSprint = sprintHeld && isMovingForward && stamina != null && stamina.HasStamina();

        float targetSpeed = canSprint ? sprintSpeed : walkSpeed;

        if (canSprint)
        {
            stamina.StartDraining();
        }
        else
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

        if (firePressed)
        {
            currentWeapon.TryFire();
        }
    }

    public void SetSensitivity(float x, float y)
    {
        sensitivityX = x;
        sensitivityY = y;
    }

}
