using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
public class CinemachineSprintFX : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Reference to the Cinemachine Virtual Camera (VCam_Player)")]
    public CinemachineCamera vcam;

    [Tooltip("The Transform Cinemachine follows (CameraAnchor on player)")]
    public Transform cameraAnchor;

    [Tooltip("Reference to the player controller (must expose IsSprinting & IsMoving)")]
    public CipherController playerController;

    [Header("FOV Settings")]
    public float normalFOV = 70f;
    public float sprintFOV = 85f;
    [Tooltip("Higher = faster FOV interpolation")]
    public float fovSmoothSpeed = 8f;

    [Header("Head Bob Settings")]
    [Tooltip("Vertical amplitude in meters")]
    public float bobAmplitude = 0.045f;      // meters
    [Tooltip("Frequency in cycles per second")]
    public float bobFrequency = 12f;         // Hz
    [Tooltip("How fast bob transitions in/out")]
    public float bobTransitionSpeed = 6f;

    [Header("Movement gating")]
    [Tooltip("Minimum movement magnitude to consider the player is moving")]
    public float minMoveThreshold = 0.1f;

    // internal
    private float bobTimer = 0f;
    private float anchorDefaultY;
    private float currentFOV;
    private CinemachineBrain brain;

    void Reset()
    {
        // sensible default values
        normalFOV = 70f;
        sprintFOV = 85f;
        fovSmoothSpeed = 8f;
        bobAmplitude = 0.045f;
        bobFrequency = 12f;
        bobTransitionSpeed = 6f;
        minMoveThreshold = 0.1f;
    }

    void Awake()
    {
        if (vcam == null)
        {
            Debug.LogError("[CinemachineSprintFX] vcam reference is missing on " + name);
            enabled = false;
            return;
        }

        // cache starting FOV from vcam lens
        currentFOV = vcam.Lens.FieldOfView;

        // optional: find CinemachineBrain
        brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
    }

    void Update()
    {
        if (playerController == null || cameraAnchor == null || vcam == null) return;

        bool sprinting = playerController.IsSprinting && playerController.IsMoving && playerController.StaminaAvailable();
        // ----- FOV interpolation -----
        float targetFOV = sprinting ? sprintFOV : normalFOV;
        currentFOV = Mathf.Lerp(currentFOV, targetFOV, 1f - Mathf.Exp(-fovSmoothSpeed * Time.deltaTime));
        vcam.Lens.FieldOfView = currentFOV;

        // ----- Head bob -----
        if (sprinting)
        {
            // advance bob timer at bobFrequency cycles per second
            bobTimer += Time.deltaTime * bobFrequency * Mathf.PI * 2f; // convert freq to radians
            float bobOffset = Mathf.Sin(bobTimer) * bobAmplitude;

            Vector3 local = cameraAnchor.localPosition;
            float targetY = anchorDefaultY + bobOffset;
            local.y = Mathf.Lerp(local.y, targetY, Time.deltaTime * bobTransitionSpeed);
            cameraAnchor.localPosition = local;
        }
        else
        {
            // smoothly return anchor to default
            Vector3 local = cameraAnchor.localPosition;
            local.y = Mathf.Lerp(local.y, anchorDefaultY, Time.deltaTime * bobTransitionSpeed);
            cameraAnchor.localPosition = local;
            // gently reset bobTimer to avoid sudden jump next time
            bobTimer = 0f;
        }
    }

    public void BindCipherPlayer(CipherController cipher)
    {
        playerController = cipher;
        cameraAnchor = cipher.cameraHolder;
        vcam.Target.TrackingTarget = cameraAnchor;
        anchorDefaultY = cameraAnchor.localPosition.y;
    }

    #if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (cameraAnchor != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cameraAnchor.position, 0.05f);
        }
    }
    #endif
}
