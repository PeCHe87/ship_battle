using UnityEngine;

public class ShipPlayerBarrier : MonoBehaviour
{
    const string ShipLayerName = "Ship";
    const string ShipPlayerBlockLayerName = "ShipPlayerBlock";

    void Awake()
    {
        int shipLayer = LayerMask.NameToLayer(ShipLayerName);
        int shipPlayerBlockLayer = LayerMask.NameToLayer(ShipPlayerBlockLayerName);

        if (shipLayer < 0 || shipPlayerBlockLayer < 0)
        {
            Debug.LogError(
                "[ShipPlayerBarrier] Missing layers. Need Ship and ShipPlayerBlock. Skipping barrier setup.",
                this);
            return;
        }

        Collider[] sourceColliders = GetComponentsInChildren<Collider>(true);

        var barrierObject = new GameObject("PlayerBarrier");
        barrierObject.layer = shipPlayerBlockLayer;
        barrierObject.transform.SetParent(transform, false);

        var barrierBody = barrierObject.AddComponent<Rigidbody>();
        barrierBody.isKinematic = true;
        barrierBody.useGravity = false;

        for (int i = 0; i < sourceColliders.Length; i++)
        {
            Collider source = sourceColliders[i];

            // Boarding and other triggers stay on Default.
            if (source.isTrigger)
                continue;

            if (!TryCloneCollider(source, barrierObject.transform, shipPlayerBlockLayer))
            {
                Debug.LogWarning(
                    $"[ShipPlayerBarrier] Unsupported collider type '{source.GetType().Name}' on '{source.name}'. Skipped.",
                    source);
                continue;
            }

            source.gameObject.layer = shipLayer;
        }
    }

    static bool TryCloneCollider(Collider source, Transform barrierRoot, int barrierLayer)
    {
        var cloneObject = new GameObject($"{source.name}_Barrier");
        cloneObject.layer = barrierLayer;
        cloneObject.transform.SetParent(barrierRoot, false);

        Transform sourceTransform = source.transform;
        cloneObject.transform.SetPositionAndRotation(sourceTransform.position, sourceTransform.rotation);
        cloneObject.transform.localScale = sourceTransform.lossyScale;

        switch (source)
        {
            case BoxCollider box:
            {
                var clone = cloneObject.AddComponent<BoxCollider>();
                clone.center = box.center;
                clone.size = box.size;
                clone.sharedMaterial = box.sharedMaterial;
                return true;
            }
            case SphereCollider sphere:
            {
                var clone = cloneObject.AddComponent<SphereCollider>();
                clone.center = sphere.center;
                clone.radius = sphere.radius;
                clone.sharedMaterial = sphere.sharedMaterial;
                return true;
            }
            case CapsuleCollider capsule:
            {
                var clone = cloneObject.AddComponent<CapsuleCollider>();
                clone.center = capsule.center;
                clone.radius = capsule.radius;
                clone.height = capsule.height;
                clone.direction = capsule.direction;
                clone.sharedMaterial = capsule.sharedMaterial;
                return true;
            }
            case MeshCollider mesh:
            {
                var clone = cloneObject.AddComponent<MeshCollider>();
                clone.sharedMesh = mesh.sharedMesh;
                clone.convex = mesh.convex;
                clone.sharedMaterial = mesh.sharedMaterial;
                return true;
            }
            default:
                Object.Destroy(cloneObject);
                return false;
        }
    }
}
