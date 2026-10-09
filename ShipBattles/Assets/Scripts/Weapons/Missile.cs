using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Missile : MonoBehaviour
{
    [SerializeField, Tooltip("Seconds before the missile is destroyed if it never hits anything.")]
    float lifetime = 8f;

    [SerializeField, Tooltip("Played at the missile position when it hits a ship or another missile.")]
    GameObject explosionPrefab;

    [SerializeField, Tooltip("Seconds to ignore the firing ship so the shot can clear the muzzle before friendly hits count.")]
    float ownerExitIgnoreDuration = 0.2f;

    Rigidbody _rb;
    Collider _collider;
    Vector3 _velocityBeforePhysics;
    bool _exploded;
    Collider[] _ignoredOwnerColliders;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        // Space vacuum: no gravity or damping so a one-shot launch velocity coasts forever.
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        // Top-down arena: keep travel on the XZ plane even if a wall contact normal has a Y component.
        _rb.constraints = RigidbodyConstraints.FreezePositionY;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        // Cache before PhysX resolves contacts so wall hits can reflect the true inbound velocity.
        _velocityBeforePhysics = _rb.linearVelocity;
    }

    /// <summary>Applies an instantaneous world-space velocity. With zero damping the missile coasts.</summary>
    public void Launch(Vector3 worldVelocity)
    {
        worldVelocity.y = 0f;
        _rb.angularVelocity = Vector3.zero;
        _rb.linearVelocity = worldVelocity;
        _velocityBeforePhysics = worldVelocity;
    }

    /// <summary>
    /// Briefly ignores the firing ship's colliders so the muzzle exit does not count as a hit;
    /// after that, own-ship collisions explode normally.
    /// </summary>
    public void IgnoreOwnerWhileExiting(Collider[] ownerColliders)
    {
        if (_collider == null || ownerColliders == null || ownerColliders.Length == 0)
            return;

        _ignoredOwnerColliders = ownerColliders;
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] != null)
                Physics.IgnoreCollision(_collider, ownerColliders[i], true);
        }

        CancelInvoke(nameof(ClearOwnerIgnore));
        Invoke(nameof(ClearOwnerIgnore), ownerExitIgnoreDuration);
    }

    void ClearOwnerIgnore()
    {
        if (_collider == null || _ignoredOwnerColliders == null)
            return;

        for (int i = 0; i < _ignoredOwnerColliders.Length; i++)
        {
            if (_ignoredOwnerColliders[i] != null)
                Physics.IgnoreCollision(_collider, _ignoredOwnerColliders[i], false);
        }

        _ignoredOwnerColliders = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_exploded)
            return;

        if (collision.collider.GetComponentInParent<ScreenBoundary>() != null)
        {
            BounceOffWall(collision);
            return;
        }

        if (collision.collider.GetComponentInParent<ShipController>() != null
            || collision.collider.GetComponentInParent<Missile>() != null)
        {
            Explode();
        }
    }

    void BounceOffWall(Collision collision)
    {
        Vector3 normal = collision.GetContact(0).normal;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f)
            return;
        normal.Normalize();

        Vector3 inbound = _velocityBeforePhysics;
        inbound.y = 0f;
        // Pong: reverse only the horizontal component along the wall normal, keep full speed.
        _rb.linearVelocity = Vector3.Reflect(inbound, normal);
    }

    void Explode()
    {
        if (_exploded)
            return;
        _exploded = true;

        if (explosionPrefab != null)
        {
            Instantiate(
                explosionPrefab,
                transform.position,
                explosionPrefab.transform.rotation);
        }

        Destroy(gameObject);
    }
}
