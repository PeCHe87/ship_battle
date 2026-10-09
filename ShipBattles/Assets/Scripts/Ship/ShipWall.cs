using UnityEngine;

public class ShipWall : MonoBehaviour
{
    [SerializeField] float maxShieldHealth = 50f;

    [SerializeField, Tooltip("Shown while the shield has remaining health.")]
    GameObject shieldActiveVisual;

    [SerializeField, Tooltip("Shown when the shield is broken (health is zero).")]
    GameObject shieldBrokenVisual;

    [SerializeField, Tooltip("Played once at this wall when the shield reaches zero.")]
    GameObject shieldBrokenVfxPrefab;

    float _currentShieldHealth;
    ShipHealth _shipHealth;

    public float MaxShieldHealth => maxShieldHealth;
    public float CurrentShieldHealth => _currentShieldHealth;
    public bool IsShieldActive => _currentShieldHealth > 0f;

    void Awake()
    {
        _currentShieldHealth = maxShieldHealth;
        _shipHealth = GetComponentInParent<ShipHealth>();
        RefreshVisuals();
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
            return;

        bool wasActive = IsShieldActive;

        if (_currentShieldHealth > 0f)
        {
            float absorbed = Mathf.Min(_currentShieldHealth, amount);
            _currentShieldHealth -= absorbed;
            amount -= absorbed;
        }

        if (wasActive && !IsShieldActive)
        {
            SpawnShieldBrokenVfx();
            RefreshVisuals();
        }

        if (amount > 0f && _shipHealth != null)
            _shipHealth.TakeDamage(amount);
    }

    void RefreshVisuals()
    {
        bool active = IsShieldActive;
        if (shieldActiveVisual != null)
            shieldActiveVisual.SetActive(active);
        if (shieldBrokenVisual != null)
            shieldBrokenVisual.SetActive(!active);
    }

    void SpawnShieldBrokenVfx()
    {
        if (shieldBrokenVfxPrefab == null)
            return;

        Instantiate(
            shieldBrokenVfxPrefab,
            transform.position,
            shieldBrokenVfxPrefab.transform.rotation);
    }
}
