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

    CharacterController _controller;
    PlayerShipSensor _shipSensor;
    float _verticalVelocity;
    Vector3 _prevShipPosition;
    float _prevShipYaw;
    bool _hasShipPose;

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

        Vector3 moveDir = new Vector3(input.x, 0f, input.y);
        if (moveDir.sqrMagnitude > inputDeadzone * inputDeadzone)
        {
            moveDir.Normalize();

            Quaternion targetRotation = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotateSpeed * Time.deltaTime);
        }
        else
        {
            moveDir = Vector3.zero;
        }

        if (_shipSensor != null && _shipSensor.IsInside)
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -1f;
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
        else
        {
            _verticalVelocity = 0f;
        }

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = _verticalVelocity;
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
