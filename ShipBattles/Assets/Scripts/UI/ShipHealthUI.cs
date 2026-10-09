using UnityEngine;
using UnityEngine.UI;

public class ShipHealthUI : MonoBehaviour
{
    [SerializeField] int shipId = 1;
    [SerializeField] Slider healthSlider;

    ShipHealth _shipHealth;

    void Awake()
    {
        if (healthSlider == null)
            healthSlider = GetComponentInChildren<Slider>();
    }

    void Start()
    {
        _shipHealth = FindShipHealth(shipId);
        if (_shipHealth == null)
            return;

        _shipHealth.HealthChanged += Refresh;
        Refresh(_shipHealth.CurrentHealth, _shipHealth.MaxHealth);
    }

    void OnDestroy()
    {
        if (_shipHealth != null)
            _shipHealth.HealthChanged -= Refresh;
    }

    void Refresh(float current, float max)
    {
        if (healthSlider == null)
            return;

        healthSlider.value = max > 0f ? current / max : 0f;
    }

    static ShipHealth FindShipHealth(int id)
    {
        ShipHealth[] ships = FindObjectsByType<ShipHealth>(FindObjectsSortMode.None);
        for (int i = 0; i < ships.Length; i++)
        {
            ShipIdentity identity = ships[i].GetComponent<ShipIdentity>();
            if (identity != null && identity.Id == id)
                return ships[i];
        }

        return null;
    }
}
