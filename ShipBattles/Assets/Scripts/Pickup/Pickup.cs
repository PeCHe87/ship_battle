using System.Collections;
using UnityEngine;

public abstract class Pickup : MonoBehaviour
{
    [SerializeField, Tooltip("Seconds before the pickup despawns if never collected. 0 = never.")]
    float lifetime = 20f;

    [Header("Collect Audio")]
    [SerializeField, Tooltip("Optional. Played at the pickup when collected. Leave empty for no sound.")]
    AudioClip collectSfx;

    [SerializeField, Range(0f, 3f), Tooltip("Volume for Collect Sfx only (on top of the SFX mixer fader).")]
    float collectSfxVolume = 1f;

    [Header("Collect Presentation")]
    [SerializeField, Tooltip("Visual root hidden when the pickup is collected.")]
    GameObject art;

    [SerializeField, Tooltip("VFX root activated on collect. Should start inactive in the prefab.")]
    GameObject collectVfx;

    [SerializeField, Tooltip("Seconds to keep the pickup alive after collect so the VFX can play.")]
    float collectVfxDuration = 1.5f;

    PickupSpawner _spawner;
    bool _consumed;
    Coroutine _lifetimeRoutine;

    public void Initialize(PickupSpawner spawner)
    {
        _spawner = spawner;
    }

    void Start()
    {
        if (lifetime > 0f)
            _lifetimeRoutine = StartCoroutine(LifetimeDespawn());
    }

    void OnTriggerEnter(Collider other)
    {
        if (_consumed)
            return;

        TopDownPlayerController player = ResolvePlayer(other);
        if (player == null)
            return;

        if (!TryApply(player))
            return;

        _consumed = true;

        if (_lifetimeRoutine != null)
        {
            StopCoroutine(_lifetimeRoutine);
            _lifetimeRoutine = null;
        }

        if (collectSfx != null)
            GameAudio.PlaySfx(collectSfx, transform.position, collectSfxVolume);

        PlayCollectPresentation();
        Destroy(gameObject, Mathf.Max(0.01f, collectVfxDuration));
    }

    void OnDestroy()
    {
        if (_spawner == null)
            return;

        _spawner.NotifyDespawn();
        _spawner = null;
    }

    protected abstract bool TryApply(TopDownPlayerController player);

    static TopDownPlayerController ResolvePlayer(Collider other)
    {
        if (other == null)
            return null;

        TopDownPlayerController player = other.GetComponentInParent<TopDownPlayerController>();
        if (player != null)
            return player;

        // CharacterController trigger callbacks sometimes report the CC capsule without a Collider hierarchy.
        CharacterController controller = other.GetComponentInParent<CharacterController>();
        if (controller != null)
            return controller.GetComponent<TopDownPlayerController>();

        return null;
    }

    void PlayCollectPresentation()
    {
        if (art != null)
            art.SetActive(false);

        Collider trigger = GetComponent<Collider>();
        if (trigger != null)
            trigger.enabled = false;

        RotateY spin = GetComponent<RotateY>();
        if (spin != null)
            spin.enabled = false;

        if (collectVfx == null)
            return;

        collectVfx.SetActive(true);

        ParticleSystem[] particles = collectVfx.GetComponentsInChildren<ParticleSystem>(true);
        for (int i = 0; i < particles.Length; i++)
            particles[i].Play(true);
    }

    IEnumerator LifetimeDespawn()
    {
        yield return new WaitForSeconds(lifetime);
        if (!_consumed)
            Destroy(gameObject);
    }
}
