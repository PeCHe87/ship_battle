using UnityEngine;

public class HealthPickup : Pickup
{
    [SerializeField, Tooltip("Hull health restored on the collector's initial (home) ship.")]
    float healAmount = 25f;

    protected override bool TryApply(TopDownPlayerController player)
    {
        if (player == null)
            return false;

        PlayerShipSensor sensor = player.GetComponent<PlayerShipSensor>();
        if (sensor == null || sensor.InitialShipId == PlayerShipSensor.NoShipId)
            return false;

        ShipHealth health = FindShipHealth(sensor.InitialShipId);
        if (health == null || health.IsDestroyed)
            return false;

        // Always collect when the player has a valid home ship; Heal no-ops at full HP.
        health.Heal(healAmount);
        return true;
    }

    static ShipHealth FindShipHealth(int shipId)
    {
        ShipHealth[] ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipIdentity identity = ships[i].GetComponent<ShipIdentity>();
            if (identity != null && identity.Id == shipId)
                return ships[i];
        }

        return null;
    }
}
