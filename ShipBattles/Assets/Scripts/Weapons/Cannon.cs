using UnityEngine;
using UnityEngine.InputSystem;

public class Cannon : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] Missile missilePrefab;
    [SerializeField] Transform bulletSpawn;
    [SerializeField] InputActionReference fireAction;
    [SerializeField, Tooltip("Horizontal distance the player must be within to fire this cannon.")]
    float interactRadius = 3f;
    [SerializeField, Tooltip("World-space speed applied once at the muzzle.")]
    float muzzleSpeed = 20f;
    [SerializeField, Tooltip("Minimum seconds between shots.")]
    float fireCooldown = 0.35f;
    [SerializeField, Tooltip("Emission applied to the cannon mesh while the player is in range.")]
    Color readyEmission = new Color(2.2f, 1.1f, 0.2f, 1f);

    float _nextFireTime;
    TopDownPlayerController _player;
    Collider _cannonCollider;
    MeshRenderer _cannonRenderer;
    MaterialPropertyBlock _propertyBlock;
    bool _playerInRange;

    void Awake()
    {
        _player = FindFirstObjectByType<TopDownPlayerController>();
        _cannonCollider = GetComponent<Collider>();
        _cannonRenderer = GetComponent<MeshRenderer>();
        _propertyBlock = new MaterialPropertyBlock();

        if (bulletSpawn == null)
        {
            Transform found = transform.Find("BulletSpawn");
            if (found != null)
                bulletSpawn = found;
        }

        ApplyReadyVisuals(false);
    }

    void OnEnable()
    {
        // Shared across cannons; do not Disable on OnDisable or one cannon would mute the rest.
        if (fireAction != null && fireAction.action != null)
            fireAction.action.Enable();
    }

    void Update()
    {
        bool inRange = IsPlayerInRange();
        if (inRange != _playerInRange)
        {
            _playerInRange = inRange;
            ApplyReadyVisuals(inRange);
        }

        if (fireAction == null || fireAction.action == null)
            return;
        if (!fireAction.action.WasPressedThisFrame())
            return;
        if (!inRange)
            return;

        TryFire();
    }

    bool IsPlayerInRange()
    {
        if (_player == null)
            return false;

        Vector3 toPlayer = _player.transform.position - transform.position;
        toPlayer.y = 0f;
        return toPlayer.sqrMagnitude <= interactRadius * interactRadius;
    }

    void TryFire()
    {
        if (Time.time < _nextFireTime || missilePrefab == null || bulletSpawn == null)
            return;

        _nextFireTime = Time.time + fireCooldown;

        Missile missile = Instantiate(missilePrefab, bulletSpawn.position, bulletSpawn.rotation);

        ShipController ownerShip = GetComponentInParent<ShipController>();
        if (ownerShip != null)
            missile.IgnoreOwnerWhileExiting(ownerShip.GetComponentsInChildren<Collider>());
        else if (_cannonCollider != null)
        {
            Collider missileCollider = missile.GetComponent<Collider>();
            if (missileCollider != null)
                Physics.IgnoreCollision(missileCollider, _cannonCollider);
        }

        missile.Launch(bulletSpawn.forward * muzzleSpeed);
    }

    void ApplyReadyVisuals(bool ready)
    {
        if (_cannonRenderer == null)
            return;

        _cannonRenderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor(EmissionColorId, ready ? readyEmission : Color.black);
        _cannonRenderer.SetPropertyBlock(_propertyBlock);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
