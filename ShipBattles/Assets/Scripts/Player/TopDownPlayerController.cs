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

    CharacterController _controller;
    PlayerShipSensor _shipSensor;
    float _verticalVelocity;
    Vector3 _prevShipPosition;
    float _prevShipYaw;
    bool _hasShipPose;
    Vector3 _spaceVelocity;
    bool _wasInside;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _shipSensor = GetComponent<PlayerShipSensor>();
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        CacheShipPose();
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        if (moveAction != null && moveAction.action != null)
            input = moveAction.action.ReadValue<Vector2>();

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

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    void UpdateSpaceMovement(Vector3 inputDir, bool hasInput)
    {
        _verticalVelocity = 0f;

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

        if (_controller.isGrounded)
        {
            Vector3 worldOffset = transform.position - _prevShipPosition;
            worldOffset.y = 0f;
            Vector3 localOffset = Quaternion.Euler(0f, -_prevShipYaw, 0f) * worldOffset;
            Vector3 targetPosition = ship.position + Quaternion.Euler(0f, ship.eulerAngles.y, 0f) * localOffset;
            targetPosition.y = transform.position.y;

            // Hard-attach: CharacterController.Move collision-resolves and slides on a rotating deck.
            bool wasEnabled = _controller.enabled;
            _controller.enabled = false;
            transform.position = targetPosition;
            _controller.enabled = wasEnabled;

            if (!Mathf.Approximately(yawDelta, 0f))
                transform.Rotate(0f, yawDelta, 0f, Space.World);
        }

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
