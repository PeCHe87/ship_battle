using UnityEngine;

public class PlayerShipSensor : MonoBehaviour
{
    public const int NoShipId = -1;

    [SerializeField] bool logShipTriggers;
    [SerializeField] GameObject helmet;
    [SerializeField] int initialShipId = NoShipId;

    public bool IsInside { get; private set; }
    public int ShipId { get; private set; } = NoShipId;
    public ShipIdentity CurrentShip { get; private set; }

    // Walk-in hits Exit then Enter while still overlapping Exit. Disarm Exit until that ends.
    int _exitOverlapCount;
    bool _exitArmed = true;

    void Start()
    {
        if (initialShipId != NoShipId)
            TryEnterShipById(initialShipId);

        UpdateHelmetVisibility();
    }

    void TryEnterShipById(int id)
    {
        ShipIdentity[] ships = FindObjectsByType<ShipIdentity>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].Id != id)
                continue;

            CurrentShip = ships[i];
            ShipId = id;
            IsInside = true;
            _exitArmed = true;
            return;
        }

        Debug.LogWarning($"[PlayerShipSensor] No ship found with id {id}", this);
    }

    void OnTriggerEnter(Collider other)
    {
        var zone = other.GetComponent<ShipBoardingZone>();
        if (zone == null || zone.Ship == null)
            return;

        if (zone.ZoneKind == ShipBoardingZone.Kind.Enter)
        {
            CurrentShip = zone.Ship;
            ShipId = zone.Ship.Id;
            IsInside = true;
            _exitArmed = _exitOverlapCount == 0;
            UpdateHelmetVisibility();
            LogTrigger("Enter", ShipId);
            return;
        }

        if (zone.ZoneKind == ShipBoardingZone.Kind.Exit)
        {
            _exitOverlapCount++;
            TryExit(zone.Ship);
        }
    }

    void OnTriggerExit(Collider other)
    {
        var zone = other.GetComponent<ShipBoardingZone>();
        if (zone == null || zone.Ship == null)
            return;

        if (zone.ZoneKind != ShipBoardingZone.Kind.Exit)
            return;

        _exitOverlapCount = Mathf.Max(0, _exitOverlapCount - 1);
        _exitArmed = true;

        // Left Exit without a fresh OnTriggerEnter (still overlapping since board):
        // clear only when moving outward away from the ship.
        if (IsInside && zone.Ship.Id == ShipId && IsOutsideExit(zone.Ship.transform, other.transform.position))
            CompleteExit(zone.Ship.Id);
    }

    void TryExit(ShipIdentity ship)
    {
        if (!IsInside || ship.Id != ShipId || !_exitArmed)
            return;

        CompleteExit(ship.Id);
    }

    void CompleteExit(int exitedShipId)
    {
        ClearShip();
        SnapToGroundHeight();
        LogTrigger("Exit", exitedShipId);
    }

    void ClearShip()
    {
        CurrentShip = null;
        ShipId = NoShipId;
        IsInside = false;
        _exitArmed = true;
        UpdateHelmetVisibility();
    }

    void UpdateHelmetVisibility()
    {
        if (helmet == null)
            return;

        helmet.SetActive(!IsInside);
    }

    void SnapToGroundHeight()
    {
        var controller = GetComponent<CharacterController>();
        Vector3 position = transform.position;
        position.y = 1f;

        if (controller != null)
        {
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            transform.position = position;
            controller.enabled = wasEnabled;
        }
        else
        {
            transform.position = position;
        }
    }

    bool IsOutsideExit(Transform ship, Vector3 exitPosition)
    {
        Vector3 shipFlat = ship.position;
        shipFlat.y = 0f;

        Vector3 playerFlat = transform.position;
        playerFlat.y = 0f;

        Vector3 exitFlat = exitPosition;
        exitFlat.y = 0f;

        float playerDist = Vector3.Distance(playerFlat, shipFlat);
        float exitDist = Vector3.Distance(exitFlat, shipFlat);
        return playerDist + 0.25f >= exitDist;
    }

    void LogTrigger(string action, int shipId)
    {
        if (!logShipTriggers)
            return;

        Debug.Log($"[PlayerShipSensor] {action} ship id {shipId}", this);
    }
}
