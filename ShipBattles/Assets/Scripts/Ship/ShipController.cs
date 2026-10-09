using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotateSpeed = 90f;

    bool _moveForward;
    bool _moveBackward;
    bool _rotateLeft;
    bool _rotateRight;

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
    }

    void Update()
    {
        float move = 0f;
        if (_moveForward) move += 1f;
        if (_moveBackward) move -= 1f;

        float yaw = 0f;
        if (_rotateLeft) yaw -= 1f;
        if (_rotateRight) yaw += 1f;

        if (yaw != 0f)
            transform.Rotate(0f, yaw * rotateSpeed * Time.deltaTime, 0f, Space.World);

        if (move != 0f)
        {
            Vector3 delta = transform.forward * (move * moveSpeed * Time.deltaTime);
            delta.y = 0f;
            transform.position += delta;
        }
    }
}
