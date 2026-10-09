using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class TopDownPlayerController : MonoBehaviour
{
    [SerializeField] InputActionReference moveAction;
    [SerializeField] RotateY rotatingLevel;
    [SerializeField] float moveSpeed = 6f;
    [SerializeField] float rotateSpeed = 720f;
    [SerializeField] float inputDeadzone = 0.1f;

    CharacterController _controller;
    float _verticalVelocity;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();
    }

    void Update()
    {
        ApplyLevelCarry();

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

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += Physics.gravity.y * Time.deltaTime;

        Vector3 velocity = moveDir * moveSpeed;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);
    }

    void ApplyLevelCarry()
    {
        if (rotatingLevel == null || !_controller.isGrounded)
            return;

        float yawDelta = rotatingLevel.Speed * Time.deltaTime;
        Vector3 pivot = rotatingLevel.transform.position;
        pivot.y = transform.position.y;

        Vector3 offset = transform.position - pivot;
        offset.y = 0f;
        Vector3 carried = Quaternion.AngleAxis(yawDelta, Vector3.up) * offset;
        Vector3 carryDelta = (pivot + carried) - transform.position;
        carryDelta.y = 0f;

        if (carryDelta.sqrMagnitude > 0f)
            _controller.Move(carryDelta);

        transform.Rotate(0f, yawDelta, 0f, Space.World);
    }
}
