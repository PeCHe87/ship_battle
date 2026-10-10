using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [SerializeField] float maxHealth = 100f;

    [SerializeField, Tooltip("Prefab or ParticleSystem spawned at the ship when health reaches zero.")]
    ParticleSystem deathParticles;

    [SerializeField, Tooltip("Played at the ship when it is destroyed.")]
    AudioClip deathSfx;

    [SerializeField, Range(0f, 3f), Tooltip("Volume for Death Sfx only (on top of the SFX mixer fader).")]
    float deathSfxVolume = 1.5f;

    float _currentHealth;
    bool _isDestroyed;
    ShipController _shipController;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => _currentHealth;
    public bool IsDestroyed => _isDestroyed;

    public event Action<float, float> HealthChanged;
    public event Action<ShipHealth> Destroyed;

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

    public bool Heal(float amount)
    {
        if (_isDestroyed || amount <= 0f || _currentHealth >= maxHealth)
            return false;

        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        NotifyHealthChanged();
        return true;
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

        if (deathParticles != null)
        {
            ParticleSystem deathFx = Instantiate(deathParticles, transform.position, transform.rotation);
            deathFx.Play();
        }

        if (deathSfx != null)
            GameAudio.PlaySfx(deathSfx, transform.position, deathSfxVolume);

        // Fire before deactivate so listeners can still read identity/components on this ship.
        Destroyed?.Invoke(this);
        gameObject.SetActive(false);
    }
}
