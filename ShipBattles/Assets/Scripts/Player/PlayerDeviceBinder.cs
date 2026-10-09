using System.Collections;
using System.Collections.Generic;
using InControl;
using UnityEngine;
using UnityEngine.InputSystem;
using InControlDevice = InControl.InputDevice;
using UnityInputDevice = UnityEngine.InputSystem.InputDevice;

/// <summary>
/// Pairs attached gamepads/joysticks to scene players (by name order) and
/// destroys extra player instances when fewer pads are connected.
/// </summary>
public class PlayerDeviceBinder : MonoBehaviour
{
    const int MaxWaitFrames = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindFirstObjectByType<PlayerDeviceBinder>() != null)
            return;

        var go = new GameObject("PlayerDeviceBinder");
        go.AddComponent<PlayerDeviceBinder>();
    }

    void Awake()
    {
        EnsureInControlManager();
    }

    IEnumerator Start()
    {
        // Wait until pads show up (InControl and/or Unity Input System), then pair.
        for (int i = 0; i < MaxWaitFrames; i++)
        {
            if (CountDetectedPads() > 0)
                break;
            yield return null;
        }

        PairAndCull();
    }

    static void EnsureInControlManager()
    {
        if (FindFirstObjectByType<InControlManager>() != null)
            return;

        var go = new GameObject("InControl Manager");
        go.AddComponent<InControlManager>();
    }

    static int CountDetectedPads()
    {
        return CollectInControlPads().Count + CollectUnityPads().Count;
    }

    static List<InControlDevice> CollectInControlPads()
    {
        var pads = new List<InControlDevice>();
        foreach (InControlDevice device in InputManager.Devices)
        {
            if (device == null || !device.IsAttached)
                continue;
            if (!IsPlayableInControlPad(device))
                continue;
            pads.Add(device);
        }

        pads.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
        return pads;
    }

    static bool IsPlayableInControlPad(InControlDevice device)
    {
        switch (device.DeviceClass)
        {
            case InputDeviceClass.Keyboard:
            case InputDeviceClass.Mouse:
            case InputDeviceClass.TouchScreen:
                return false;
            case InputDeviceClass.Controller:
            case InputDeviceClass.ArcadeStick:
            case InputDeviceClass.ArcadePad:
            case InputDeviceClass.FlightStick:
            case InputDeviceClass.Unknown:
                // Unknown covers generic DirectInput joysticks InControl still attaches.
                return true;
            default:
                return false;
        }
    }

    static List<UnityInputDevice> CollectUnityPads()
    {
        var pads = new List<UnityInputDevice>();

        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad != null && gamepad.added)
                pads.Add(gamepad);
        }

        foreach (Joystick joystick in Joystick.all)
        {
            if (joystick == null || !joystick.added)
                continue;
            // Gamepad already listed above; skip duplicates that also appear as Joystick.
            if (joystick is Gamepad)
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
        players.Sort((a, b) => string.CompareOrdinal(a.name, b.name));

        List<InControlDevice> inControlPads = CollectInControlPads();
        List<UnityInputDevice> unityPads = CollectUnityPads();

        // Use whichever backend reports more pads (InControl often misses Joystick-only devices).
        int padCount = Mathf.Max(inControlPads.Count, unityPads.Count);
        // 0 pads → keep player_1 on keyboard; otherwise one player per pad.
        int keepCount = padCount == 0 ? 1 : Mathf.Min(padCount, players.Count);

        for (int i = 0; i < players.Count; i++)
        {
            if (i >= keepCount)
            {
                Destroy(players[i].gameObject);
                continue;
            }

            players[i].Device = i < inControlPads.Count ? inControlPads[i] : null;
            players[i].UnityDevice = i < unityPads.Count ? unityPads[i] : null;
        }
    }
}
