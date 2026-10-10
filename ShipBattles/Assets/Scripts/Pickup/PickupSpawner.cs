using System;
using UnityEngine;

public class PickupSpawner : MonoBehaviour
{
    [Serializable]
    public class PickupEntry
    {
        public Pickup prefab;
        [Min(0f)] public float weight = 1f;
    }

    [SerializeField] PickupEntry[] pickups;

    [SerializeField, Min(1)]
    int maxActive = 3;

    [SerializeField, Min(0.1f), Tooltip("Seconds between spawn attempts.")]
    float spawnInterval = 10f;

    [SerializeField, Tooltip("World-space center of the spawn box.")]
    Vector3 spawnCenter;

    [SerializeField, Tooltip("Half-extents of the spawn box on X and Z. Y is ignored; spawnHeight is used.")]
    Vector3 spawnHalfExtents = new Vector3(20f, 0f, 12f);

    [SerializeField, Tooltip("World Y used for all spawned pickups.")]
    float spawnHeight = 1f;

    int _activeCount;
    float _nextSpawnTime;

    void Start()
    {
        _nextSpawnTime = Time.time + spawnInterval;
    }

    void Update()
    {
        if (Time.time < _nextSpawnTime)
            return;

        _nextSpawnTime = Time.time + spawnInterval;

        if (_activeCount >= maxActive)
            return;

        Pickup prefab = PickWeightedPrefab();
        if (prefab == null)
            return;

        Vector3 position = RandomPointInSpawnBox();
        Pickup instance = Instantiate(prefab, position, prefab.transform.rotation);
        instance.Initialize(this);
        _activeCount++;
    }

    public void NotifyDespawn()
    {
        _activeCount = Mathf.Max(0, _activeCount - 1);
    }

    Pickup PickWeightedPrefab()
    {
        if (pickups == null || pickups.Length == 0)
            return null;

        float total = 0f;
        for (int i = 0; i < pickups.Length; i++)
        {
            PickupEntry entry = pickups[i];
            if (entry != null && entry.prefab != null && entry.weight > 0f)
                total += entry.weight;
        }

        if (total <= 0f)
            return null;

        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;
        for (int i = 0; i < pickups.Length; i++)
        {
            PickupEntry entry = pickups[i];
            if (entry == null || entry.prefab == null || entry.weight <= 0f)
                continue;

            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.prefab;
        }

        return null;
    }

    Vector3 RandomPointInSpawnBox()
    {
        float x = UnityEngine.Random.Range(spawnCenter.x - spawnHalfExtents.x, spawnCenter.x + spawnHalfExtents.x);
        float z = UnityEngine.Random.Range(spawnCenter.z - spawnHalfExtents.z, spawnCenter.z + spawnHalfExtents.z);
        return new Vector3(x, spawnHeight, z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.35f);
        Vector3 center = new Vector3(spawnCenter.x, spawnHeight, spawnCenter.z);
        Vector3 size = new Vector3(spawnHalfExtents.x * 2f, 0.5f, spawnHalfExtents.z * 2f);
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(0.2f, 0.9f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}
