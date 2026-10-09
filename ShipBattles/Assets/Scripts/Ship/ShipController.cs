using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Move force")]
    [SerializeField] float baseMoveForce = 20f;
    [SerializeField] float moveForceIncrement = 40f;
    [SerializeField] float maxMoveForce = 80f;

    [Header("Turn speed (degrees/sec)")]
    [SerializeField] float baseTurnSpeed = 40f;
    [SerializeField] float turnSpeedIncrement = 60f;
    [SerializeField] float maxTurnSpeed = 120f;

    bool _moveForward;
    bool _moveBackward;
    bool _rotateLeft;
    bool _rotateRight;

    float _currentMoveForce;
    float _currentTurnSpeed;

    Rigidbody _body;

    public void StartMoveForward() => _moveForward = true;
    public void StopMoveForward() => _moveForward = false;

    public void StartMoveBackward() => _moveBackward = true;
    public void StopMoveBackward() => _moveBackward = false;

    public void StartRotateLeft() => _rotateLeft = true;
    public void StopRotateLeft() => _rotateLeft = false;

    public void StartRotateRight() => _rotateRight = true;
    public void StopRotateRight() => _rotateRight = false;

    public void StopAll()
    {
        _moveForward = false;
        _moveBackward = false;
        _rotateLeft = false;
        _rotateRight = false;
        _currentMoveForce = 0f;
        _currentTurnSpeed = 0f;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.centerOfMass = Vector3.zero;
    }

    void FixedUpdate()
    {
        float moveNet = 0f;
        if (_moveForward) moveNet += 1f;
        if (_moveBackward) moveNet -= 1f;

        float turnNet = 0f;
        if (_rotateRight) turnNet += 1f;
        if (_rotateLeft) turnNet -= 1f;

        if (moveNet != 0f)
        {
            if (_currentMoveForce <= 0f)
                _currentMoveForce = baseMoveForce;
            else
                _currentMoveForce = Mathf.Min(
                    maxMoveForce,
                    _currentMoveForce + moveForceIncrement * Time.fixedDeltaTime);

            _body.AddForce(transform.forward * (moveNet * _currentMoveForce), ForceMode.Force);
        }
        else
        {
            _currentMoveForce = 0f;
        }

        if (turnNet != 0f)
        {
            if (_currentTurnSpeed <= 0f)
                _currentTurnSpeed = baseTurnSpeed;
            else
                _currentTurnSpeed = Mathf.Min(
                    maxTurnSpeed,
                    _currentTurnSpeed + turnSpeedIncrement * Time.fixedDeltaTime);

            float yawDelta = turnNet * _currentTurnSpeed * Time.fixedDeltaTime;
            _body.MoveRotation(Quaternion.Euler(0f, yawDelta, 0f) * _body.rotation);

            // Pure spin: don't let floor friction shove the hull while only turning.
            if (moveNet == 0f)
                _body.linearVelocity = Vector3.zero;
        }
        else
        {
            _currentTurnSpeed = 0f;
        }

        Vector3 velocity = _body.linearVelocity;
        velocity.y = 0f;
        _body.linearVelocity = velocity;
    }
}
