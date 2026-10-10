using System.Collections;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityInputDevice = UnityEngine.InputSystem.InputDevice;

/// <summary>
/// Pairs Unity Input System gamepads/joysticks to scene players (by name order)
/// and destroys extra player instances when fewer pads are connected.
/// Survives scene reloads so pairing runs again after match reset.
/// </summary>
public class PlayerDeviceBinder : MonoBehaviour
{
    const int MaxWaitFrames = 60;

    static PlayerDeviceBinder _instance;
    Coroutine _pairRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (_instance != null)
            return;

        var go = new GameObject("PlayerDeviceBinder");
        go.AddComponent<PlayerDeviceBinder>();
    }

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInControlManager();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (_instance == this)
            _instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        RequestPairAndCull();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        RequestPairAndCull();
    }

    void RequestPairAndCull()
    {
        if (_pairRoutine != null)
            StopCoroutine(_pairRoutine);

        _pairRoutine = StartCoroutine(PairWhenReady());
    }

    IEnumerator PairWhenReady()
    {
        // Wait until Unity pads show up (or timeout), then pair.
        for (int i = 0; i < MaxWaitFrames; i++)
        {
            if (CollectUnityPads().Count > 0)
                break;
            yield return null;
        }

        // One extra frame so scene players finish Awake after LoadScene.
        yield return null;
        PairAndCull();
        _pairRoutine = null;
    }

    static void EnsureInControlManager()
    {
        if (FindFirstObjectByType<InControlManager>() != null)
            return;

        var go = new GameObject("InControl Manager");
        go.AddComponent<InControlManager>();
    }

    static List<UnityInputDevice> CollectUnityPads()
    {
        var pads = new List<UnityInputDevice>();
        var seenIds = new HashSet<int>();

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad == null || !gamepad.added)
                continue;
            if (!seenIds.Add(gamepad.deviceId))
                continue;
            pads.Add(gamepad);
        }

        foreach (Joystick joystick in Joystick.all)
        {
            if (joystick == null || !joystick.added)
                continue;
            // Gamepad already listed above; skip duplicates that also appear as Joystick.
            if (joystick is Gamepad)
                continue;
            if (!seenIds.Add(joystick.deviceId))
                continue;
            pads.Add(joystick);
        }

        pads.Sort((a, b) => a.deviceId.CompareTo(b.deviceId));
        return pads;
    }

    void PairAndCull()
    {
        var players = new List<TopDownPlayerController>(
            FindObjectsByType<TopDownPlayerController>(FindObjectsSortMode.None));
        if (players.Count == 0)
            return;

        players.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        // Unity Input System is the source of truth (InControl DDOL duplicates break resets).
        List<UnityInputDevice> unityPads = CollectUnityPads();
        int padCount = unityPads.Count;
        // 0 pads → keep player_1 on keyboard; otherwise one player per pad.
        int keepCount = padCount == 0 ? 1 : Mathf.Min(padCount, players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            if (i >= keepCount)
            {
                Destroy(players[i].gameObject);
                continue;
            }

            // Clear InControl assignment so movement never prefers a shared/stale Device.
            players[i].Device = null;
            players[i].UnityDevice = i < unityPads.Count ? unityPads[i] : null;
        }
    }
}
