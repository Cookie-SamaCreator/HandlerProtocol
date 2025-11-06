using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Main player character controller handling movement, combat, and loadout management
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Stamina))]
[RequireComponent(typeof(Health))]
public class CipherController : MonoBehaviour
{
    #region Component References
    [Header("Required Components")]
    [SerializeField, Tooltip("CharacterController reference - auto-assigned if null")] 
    private CharacterController cc;
    [Tooltip("Stamina system reference")] 
    public Stamina staminaSystem;
    [Tooltip("Health system reference")] 
    public Health healthSystem;

    [Header("Weapon Transform References")]
    [SerializeField, Tooltip("Active weapon anchor point")] 
    private Transform activeWeaponSlot;
    [SerializeField, Tooltip("First primary weapon storage")] 
    private Transform firstPrimaryWeaponSlot;
    [SerializeField, Tooltip("Second primary weapon storage")] 
    private Transform secondPrimaryWeaponSlot;
    [SerializeField, Tooltip("Secondary weapon storage")] 
    private Transform secondaryWeaponSlot;
    #endregion

    #region Movement Settings
    [Header("Movement Settings")]
    [Tooltip("Base walking speed in units per second")]
    public float walkSpeed = 5f;
    [Tooltip("Sprint speed in units per second")]
    public float sprintSpeed = 8f;
    [Tooltip("Gravity force applied to vertical movement")]
    public float gravity = -9.81f;
    [Tooltip("Jump height in units")]
    public float jumpHeight = 1.2f;
    #endregion

    #region Camera Settings
    [Header("Camera Settings")]
    [Tooltip("Camera transform parent")]
    public Transform cameraHolder;
    [Range(0.1f, 5f), Tooltip("Horizontal look sensitivity")]
    public float sensitivityX = 1.2f;
    [Range(0.1f, 5f), Tooltip("Vertical look sensitivity")]
    public float sensitivityY = 1.0f;
    [Tooltip("Minimum vertical camera angle")]
    public float minPitch = -85f;
    [Tooltip("Maximum vertical camera angle")]
    public float maxPitch = 85f;
    #endregion

    #region Weapon System
    [Header("Weapon Inventory")]
    public Weapon currentActiveWeapon;
    public Weapon currentFirstPrimary;
    public Weapon currentSecondPrimary;
    public Weapon currentSecondary;
    public List<SkillDefinition> CurrentSkills = new();
    #endregion

    #region Network State
    [Header("Network State")]
    [Tooltip("True when this is the local player's character")]
    public bool isLocalPlayer = false;
    #endregion

    #region State Properties
    /// <summary>
    /// True when the player is providing movement input above threshold
    /// </summary>
    public bool IsMoving => moveInput.magnitude > 0.1f;

    /// <summary>
    /// True when the player is sprinting and has available stamina
    /// </summary>
    public bool IsSprinting => isSprinting && (staminaSystem == null || staminaSystem.HasStamina());

    /// <summary>
    /// Checks if stamina is available for actions
    /// </summary>
    public bool StaminaAvailable() => staminaSystem == null || staminaSystem.HasStamina();
    #endregion

    #region Private State
    private float cameraPitch = 0f;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float verticalVelocity;
    private bool isSprinting;
    private bool firePressed;
    private bool wasFirePressed = false;
    private PlayerControls controls;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Cache required components
        if (cc == null) cc = GetComponent<CharacterController>();
        if (staminaSystem == null) staminaSystem = GetComponent<Stamina>();
        if (healthSystem == null) healthSystem = GetComponent<Health>();
        
        // Initialize input system
        controls = new PlayerControls();
        controls.Cipher.Enable();
        
        // Lock cursor for FPS camera
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        controls?.Cipher.Enable();
    }

    private void OnDisable()
    {
        controls?.Cipher.Disable();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        
        HandleLook();
        HandleMovement();
        HandleFire();
    }
    #endregion

    #region Input Handling
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

    #region Movement System
    private void HandleLook()
    {    
        float lookX = lookInput.x * sensitivityX;
        float lookY = lookInput.y * sensitivityY;
    
        // Rotate player body for horizontal look
        transform.Rotate(Vector3.up * lookX, Space.Self);
    
        // Update and clamp vertical camera angle
        cameraPitch -= lookY;
        cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);
    
        if (cameraHolder != null)
        {
            cameraHolder.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        // Convert input to world space movement
        Vector3 input = new(moveInput.x, 0f, moveInput.y);
        Vector3 worldMove = transform.TransformDirection(input);

        // Check sprint input and conditions
        bool sprintHeld = controls.Cipher.Sprint.ReadValue<float>() > 0.5f;
        isSprinting = sprintHeld;

        bool isMovingForward = moveInput.magnitude > 0.1f || moveInput.y > 0.1f;
        bool canSprint = sprintHeld && isMovingForward && staminaSystem != null && staminaSystem.HasStamina();

        // Apply appropriate movement speed
        float targetSpeed = canSprint ? sprintSpeed : walkSpeed;

        // Handle stamina drain
        if (canSprint)
        {
            staminaSystem.StartDraining();
        }
        else
        {
            staminaSystem.StopDraining();
        }

        // Apply gravity and grounding
        if (cc.isGrounded && verticalVelocity < 0) 
            verticalVelocity = -2f;

        verticalVelocity += gravity * Time.deltaTime;
        
        // Combine horizontal and vertical movement
        Vector3 velocity = worldMove * targetSpeed + Vector3.up * verticalVelocity;
        cc.Move(velocity * Time.deltaTime);
    }
    #endregion

    #region Combat System
    private void HandleFire()
    {
        if (currentActiveWeapon == null) return;

        bool newPress = firePressed && !wasFirePressed;
        currentActiveWeapon.TryFire(firePressed, newPress);
        wasFirePressed = firePressed;
    }
    #endregion

    #region Loadout System
    /// <summary>
    /// Sets up the player's weapon loadout from a loadout configuration
    /// </summary>
    public void SetupLoadout(CipherLoadout loadout)
    {
        // Setup first primary weapon
        var firstPrimaryDef = WeaponDatabase.Get(loadout.FirstPrimaryWeaponID);
        var weapon = Instantiate(firstPrimaryDef.WeaponModelPrefab, activeWeaponSlot);
        var w = weapon.GetComponent<Weapon>();
        w.SetupFromDefinition(firstPrimaryDef);
        currentFirstPrimary = w;
        currentActiveWeapon = w;

        // Setup second primary weapon
        var secondPrimaryDef = WeaponDatabase.Get(loadout.SecondPrimaryWeaponID);
        weapon = Instantiate(secondPrimaryDef.WeaponModelPrefab, secondPrimaryWeaponSlot);
        w = weapon.GetComponent<Weapon>();
        w.SetupFromDefinition(secondPrimaryDef);
        currentSecondPrimary = w;

        // Setup secondary weapon
        var secondaryDef = WeaponDatabase.Get(loadout.SecondaryWeaponID);
        weapon = Instantiate(secondaryDef.WeaponModelPrefab, secondaryWeaponSlot);
        w = weapon.GetComponent<Weapon>();
        w.SetupFromDefinition(secondaryDef);
        currentSecondary = w;

        // Setup skills
        CurrentSkills.Clear();
        foreach (var skillID in loadout.SkillIDs)
        {
            var skillDef = SkillDatabase.Get(skillID);
            //CurrentSkills.Add(Instantiate(skillDef.Prefab, skillContainer));
        }
    }
    #endregion

    #region Settings
    /// <summary>
    /// Updates the look sensitivity settings
    /// </summary>
    public void SetSensitivity(float x, float y)
    {
        sensitivityX = x;
        sensitivityY = y;
    }
    #endregion
}