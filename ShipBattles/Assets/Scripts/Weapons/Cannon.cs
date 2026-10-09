using UnityEngine;
using UnityEngine.InputSystem;

public class Cannon : MonoBehaviour
{
    static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    // Matches ship base top (scale.y 0.2 * cylinder half-height 1) plus a small lift above the deck.
    const float ZoneDeckOffset = 0.28f;
    const float ZoneHeightScale = 0.01f;

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
    [SerializeField] Transform zoneVisual;
    [SerializeField] MeshRenderer zoneRenderer;
    [SerializeField] Color ringIdleColor = new Color(1f, 0.55f, 0.1f, 0.22f);
    [SerializeField] Color ringReadyColor = new Color(1f, 0.75f, 0.2f, 0.45f);

    float _nextFireTime;
    TopDownPlayerController _player;
    Transform _shipTransform;
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

        ShipController ownerShip = GetComponentInParent<ShipController>();
        if (ownerShip != null)
            _shipTransform = ownerShip.transform;

        if (bulletSpawn == null)
        {
            Transform found = transform.Find("BulletSpawn");
            if (found != null)
                bulletSpawn = found;
        }

        // Cannon mesh scale is non-uniform; keep the zone unparented so the disc stays circular.
        if (zoneVisual != null)
        {
            zoneVisual.SetParent(null, false);
            SyncZoneScale();
        }

        SyncZonePosition();
        ApplyReadyVisuals(false);
    }

    void OnEnable()
    {
        // Shared across cannons; do not Disable on OnDisable or one cannon would mute the rest.
        if (fireAction != null && fireAction.action != null)
            fireAction.action.Enable();
    }

    void OnDestroy()
    {
        if (zoneVisual != null)
            Destroy(zoneVisual.gameObject);
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

    void LateUpdate()
    {
        SyncZonePosition();
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

    void SyncZoneScale()
    {
        if (zoneVisual == null)
            return;

        // Default cylinder radius is 0.5, so scale xz = diameter yields world radius = interactRadius.
        float diameter = interactRadius * 2f;
        zoneVisual.localScale = new Vector3(diameter, ZoneHeightScale, diameter);
    }

    void SyncZonePosition()
    {
        if (zoneVisual == null)
            return;

        Vector3 position = transform.position;
        position.y = _shipTransform != null
            ? _shipTransform.position.y + ZoneDeckOffset
            : transform.position.y;
        zoneVisual.SetPositionAndRotation(position, Quaternion.identity);
    }

    void ApplyReadyVisuals(bool ready)
    {
        if (_cannonRenderer != null)
        {
            _cannonRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, ready ? readyEmission : Color.black);
            _cannonRenderer.SetPropertyBlock(_propertyBlock);
        }

        if (zoneRenderer != null)
        {
            Color ringColor = ready ? ringReadyColor : ringIdleColor;
            zoneRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(BaseColorId, ringColor);
            _propertyBlock.SetColor(ColorId, ringColor);
            zoneRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
