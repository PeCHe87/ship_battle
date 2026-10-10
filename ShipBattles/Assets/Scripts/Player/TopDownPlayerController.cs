using InControl;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerShipSensor))]
public class TopDownPlayerController : MonoBehaviour
{
    [SerializeField] InputActionReference moveAction;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotateSpeed = 720f;
    [SerializeField] float inputDeadzone = 0.1f;

    [Header("Space (outside ship)")]
    [SerializeField] float spaceMaxSpeed = 6f;
    [SerializeField] float spaceAcceleration = 8f;
    [SerializeField] float spaceDeceleration = 4f;

    /// <summary>Optional InControl pad. Prefer UnityDevice when both are set.</summary>
    public InControl.InputDevice Device { get; set; }

    /// <summary>Assigned by PlayerDeviceBinder. Null = keyboard-only fallback.</summary>
    public UnityEngine.InputSystem.InputDevice UnityDevice { get; set; }

    CharacterController _controller;
    PlayerShipSensor _shipSensor;
    float _lockedY;
    Vector3 _prevShipPosition;
    float _prevShipYaw;
    bool _hasShipPose;
    Vector3 _spaceVelocity;
    bool _wasInside;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _shipSensor = GetComponent<PlayerShipSensor>();
        _lockedY = transform.position.y;
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        CacheShipPose();
    }

    void OnDisable()
    {
        // Shared InputActionReference — do not Disable here or culling one player mutes the rest.
    }

    void Update()
    {
        Vector2 input = ReadMoveInput();

        bool hasInput = input.sqrMagnitude > inputDeadzone * inputDeadzone;
        Vector3 inputDir = hasInput
            ? new Vector3(input.x, 0f, input.y).normalized
            : Vector3.zero;

        bool inside = _shipSensor != null && _shipSensor.IsInside;
        HandleModeTransition(inside, inputDir);

        if (inside)
            UpdateDeckMovement(inputDir, hasInput);
        else
            UpdateSpaceMovement(inputDir, hasInput);

        _wasInside = inside;
    }

    public bool WasInteractPressed()
    {
        if (UnityDevice is Gamepad gamepad)
            return gamepad.buttonSouth.wasPressedThisFrame;

        if (UnityDevice is Joystick joystick)
        {
            if (joystick.trigger != null && joystick.trigger.wasPressedThisFrame)
                return true;
        }

        if (Device != null)
            return Device.Action1.WasPressed;

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }

    public static TopDownPlayerController FindNearestInRange(Vector3 origin, float radius)
    {
        TopDownPlayerController[] players =
            FindObjectsByType<TopDownPlayerController>(FindObjectsSortMode.None);
        float radiusSq = radius * radius;
        TopDownPlayerController nearest = null;
        float nearestSq = float.MaxValue;

        for (int i = 0; i < players.Length; i++)
        {
            TopDownPlayerController player = players[i];
            if (player == null)
                continue;

            Vector3 toPlayer = player.transform.position - origin;
            toPlayer.y = 0f;
            float distSq = toPlayer.sqrMagnitude;
            if (distSq > radiusSq || distSq >= nearestSq)
                continue;

            nearestSq = distSq;
            nearest = player;
        }

        return nearest;
    }

    Vector2 ReadMoveInput()
    {
        // Unity device first — unique per player across scene reloads.
        if (UnityDevice is Gamepad gamepad)
            return gamepad.leftStick.ReadValue();

        if (UnityDevice is Joystick joystick)
        {
            if (joystick.stick != null)
                return joystick.stick.ReadValue();
        }

        if (Device != null)
            return (Vector2)Device.Direction;

        // Keyboard-only fallback (solo when no pads). Shared across players — avoid when UnityDevice is set.
        if (UnityDevice == null && moveAction != null && moveAction.action != null)
            return moveAction.action.ReadValue<Vector2>();

        return Vector2.zero;
    }

    void HandleModeTransition(bool inside, Vector3 inputDir)
    {
        if (inside && !_wasInside)
        {
            _spaceVelocity = Vector3.zero;
            return;
        }

        if (!inside && _wasInside)
        {
            // Seed coast from the deck move the player had when exiting.
            _spaceVelocity = inputDir * moveSpeed;
        }
    }

    void UpdateDeckMovement(Vector3 moveDir, bool hasInput)
    {
        if (hasInput)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = 0f;
        _controller.Move(velocity * Time.deltaTime);
    }

    void UpdateSpaceMovement(Vector3 inputDir, bool hasInput)
    {
        Vector3 targetVelocity = hasInput ? inputDir * spaceMaxSpeed : Vector3.zero;
        float rate = hasInput ? spaceAcceleration : spaceDeceleration;
        _spaceVelocity = Vector3.MoveTowards(
            _spaceVelocity,
            targetVelocity,
            rate * Time.deltaTime);

        Vector3 faceDir = _spaceVelocity.sqrMagnitude > inputDeadzone * inputDeadzone
            ? _spaceVelocity.normalized
            : inputDir;

        if (faceDir.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }

        Vector3 velocity = _spaceVelocity;
        velocity.y = 0f;
        _controller.Move(velocity * Time.deltaTime);
    }

    void LateUpdate()
    {
        ApplyShipCarry();
        LockY();
    }

    void LockY()
    {
        Vector3 position = transform.position;
        if (Mathf.Approximately(position.y, _lockedY))
            return;

        position.y = _lockedY;
        // Do not toggle CharacterController.enabled — that breaks boarding trigger enter/exit.
        transform.position = position;
    }

    void ApplyShipCarry()
    {
        Transform ship = _shipSensor != null && _shipSensor.IsInside && _shipSensor.CurrentShip != null
            ? _shipSensor.CurrentShip.transform
            : null;

        if (ship == null)
        {
            _hasShipPose = false;
            return;
        }

        if (!_hasShipPose)
        {
            CacheShipPose(ship);
            return;
        }

        float yawDelta = Mathf.DeltaAngle(_prevShipYaw, ship.eulerAngles.y);

        Vector3 worldOffset = transform.position - _prevShipPosition;
        worldOffset.y = 0f;
        Vector3 localOffset = Quaternion.Euler(0f, -_prevShipYaw, 0f) * worldOffset;
        Vector3 targetPosition = ship.position + Quaternion.Euler(0f, ship.eulerAngles.y, 0f) * localOffset;
        targetPosition.y = _lockedY;

        Vector3 delta = targetPosition - transform.position;
        // Skip when the ship did not move/rotate: walking is already applied by Move(),
        // and toggling/teleporting every frame breaks ShipBoardingZone triggers.
        if (delta.sqrMagnitude > 0.000001f)
            transform.position = targetPosition;

        if (!Mathf.Approximately(yawDelta, 0f))
            transform.Rotate(0f, yawDelta, 0f, Space.World);

        CacheShipPose(ship);
    }

    void CacheShipPose(Transform ship = null)
    {
        if (ship == null)
        {
            if (_shipSensor == null || !_shipSensor.IsInside || _shipSensor.CurrentShip == null)
            {
                _hasShipPose = false;
                return;
            }

            ship = _shipSensor.CurrentShip.transform;
        }

        _prevShipPosition = ship.position;
        _prevShipYaw = ship.eulerAngles.y;
        _hasShipPose = true;
    }
}
