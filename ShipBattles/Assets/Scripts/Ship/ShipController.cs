using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipController : MonoBehaviour
{
    [Header("Move force")]
    [SerializeField] float baseMoveForce = 20f;
    [SerializeField] float moveForceIncrement = 40f;
    [SerializeField] float maxMoveForce = 80f;

    [Header("Turn torque")]
    [SerializeField] float baseTurnTorque = 15f;
    [SerializeField] float turnTorqueIncrement = 30f;
    [SerializeField] float maxTurnTorque = 60f;

    bool _moveForward;
    bool _moveBackward;
    bool _rotateLeft;
    bool _rotateRight;

    float _currentMoveForce;
    float _currentTurnTorque;

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
        _currentTurnTorque = 0f;
    }

    void Awake()
    {
        _body = GetComponent<Rigidbody>();
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
            if (_currentTurnTorque <= 0f)
                _currentTurnTorque = baseTurnTorque;
            else
                _currentTurnTorque = Mathf.Min(
                    maxTurnTorque,
                    _currentTurnTorque + turnTorqueIncrement * Time.fixedDeltaTime);

            _body.AddTorque(Vector3.up * (turnNet * _currentTurnTorque), ForceMode.Force);
        }
        else
        {
            _currentTurnTorque = 0f;
        }

        Vector3 velocity = _body.linearVelocity;
        velocity.y = 0f;
        _body.linearVelocity = velocity;
    }
}
