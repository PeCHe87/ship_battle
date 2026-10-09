using UnityEngine;

[RequireComponent(typeof(ShipController))]
public class ShipControllerTesting : MonoBehaviour
{
    [SerializeField] bool moveForward;
    [SerializeField] bool moveBackward;
    [SerializeField] bool rotateLeft;
    [SerializeField] bool rotateRight;

    ShipController _ship;
    bool _wasMoveForward;
    bool _wasMoveBackward;
    bool _wasRotateLeft;
    bool _wasRotateRight;

    void Awake()
    {
        _ship = GetComponent<ShipController>();
    }

    void Update()
    {
        Sync(ref _wasMoveForward, moveForward, _ship.StartMoveForward, _ship.StopMoveForward);
        Sync(ref _wasMoveBackward, moveBackward, _ship.StartMoveBackward, _ship.StopMoveBackward);
        Sync(ref _wasRotateLeft, rotateLeft, _ship.StartRotateLeft, _ship.StopRotateLeft);
        Sync(ref _wasRotateRight, rotateRight, _ship.StartRotateRight, _ship.StopRotateRight);
    }

    static void Sync(ref bool wasActive, bool isActive, System.Action start, System.Action stop)
    {
        if (isActive == wasActive)
            return;

        if (isActive)
            start();
        else
            stop();

        wasActive = isActive;
    }
}
