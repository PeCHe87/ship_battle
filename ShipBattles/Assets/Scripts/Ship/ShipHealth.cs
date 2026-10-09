using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;

    float _currentHealth;
    bool _isDestroyed;
    ShipController _shipController;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDestroyed => _isDestroyed;

    public event Action<float, float> HealthChanged;

    void Awake()
    {
        _currentHealth = maxHealth;
        _shipController = GetComponent<ShipController>();
        NotifyHealthChanged();
    }

    public void TakeDamage(float amount)
    {
        if (_isDestroyed || amount <= 0f)
            return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        NotifyHealthChanged();

        if (_currentHealth <= 0f)
            HandleDestroyed();
    }

    void NotifyHealthChanged()
    {
        HealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    void HandleDestroyed()
    {
        if (_isDestroyed)
            return;

        _isDestroyed = true;
        if (_shipController != null)
            _shipController.StopAll();
    }
}
