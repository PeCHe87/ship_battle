using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Missile : MonoBehaviour
{
    [SerializeField, Tooltip("Seconds before the missile is destroyed if it never hits anything.")]
    float lifetime = 8f;

    Rigidbody _rb;
    Vector3 _velocityBeforePhysics;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // Space vacuum: no gravity or damping so a one-shot launch velocity coasts forever.
        _rb.useGravity = false;
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
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
        _rb.angularVelocity = Vector3.zero;
        _rb.linearVelocity = worldVelocity;
        _velocityBeforePhysics = worldVelocity;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.GetComponentInParent<ScreenBoundary>() == null)
            return;

        Vector3 normal = collision.GetContact(0).normal;
        // Pong: reverse only the component along the wall normal, keep full speed.
        _rb.linearVelocity = Vector3.Reflect(_velocityBeforePhysics, normal);
    }
}
