using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float jumpSpeed = 7f;
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundCheckRadius = 0.15f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float cameraDistance = 4f;
    [SerializeField] private float cameraHeight = 1.5f;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    private Rigidbody body;
    private Collider playerCollider;
    private Vector2 moveInput;
    private bool jumpRequested;
    private float yaw;
    private float pitch = 15f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        body.constraints |= RigidbodyConstraints.FreezeRotation;

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        moveInput = Vector2.zero;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) moveInput.y += 1f;
            if (keyboard.sKey.isPressed) moveInput.y -= 1f;
            if (keyboard.dKey.isPressed) moveInput.x += 1f;
            if (keyboard.aKey.isPressed) moveInput.x -= 1f;
            if (keyboard.spaceKey.wasPressedThisFrame) jumpRequested = true;
        }

        moveInput = Vector2.ClampMagnitude(moveInput, 1f);

        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, minPitch, maxPitch);
        }
    }

    private void FixedUpdate()
    {
        Quaternion cameraYaw = Quaternion.Euler(0f, yaw, 0f);
        Vector3 direction = cameraYaw * new Vector3(moveInput.x, 0f, moveInput.y);
        Vector3 velocity = body.linearVelocity;
        body.linearVelocity = new Vector3(direction.x * moveSpeed, velocity.y, direction.z * moveSpeed);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            body.MoveRotation(Quaternion.Slerp(body.rotation, targetRotation, turnSpeed * Time.fixedDeltaTime));
        }

        if (jumpRequested)
        {
            if (IsGrounded())
            {
                velocity = body.linearVelocity;
                velocity.y = jumpSpeed;
                body.linearVelocity = velocity;
            }

            jumpRequested = false;
        }
    }

    private bool IsGrounded()
    {
        Vector3 checkPosition = playerCollider.bounds.center;
        checkPosition.y = playerCollider.bounds.min.y + 0.05f;

        Collider[] hits = Physics.OverlapSphere(
            checkPosition, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);

        foreach (Collider hit in hits)
        {
            if (hit.attachedRigidbody != body && !hit.transform.IsChildOf(transform))
                return true;
        }

        return false;
    }

    private void LateUpdate()
    {
        if (cameraTransform == null) return;

        Vector3 target = transform.position + Vector3.up * cameraHeight;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        cameraTransform.SetPositionAndRotation(target - rotation * Vector3.forward * cameraDistance, rotation);
    }
}