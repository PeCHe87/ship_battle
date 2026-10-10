using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

public class MainScreenUI : MonoBehaviour
{
    [SerializeField] Button startButton;

    void Awake()
    {
        if (startButton == null)
            startButton = GetComponentInChildren<Button>(true);
    }

    void OnEnable()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(Close);
            startButton.Select();
        }
    }

    void OnDisable()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(Close);
    }

    void Update()
    {
        if (AnyInteractPressed())
            Close();
    }

    void Close()
    {
        gameObject.SetActive(false);
    }

    static bool AnyInteractPressed()
    {
        TopDownPlayerController[] players =
            FindObjectsByType<TopDownPlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].WasInteractPressed())
                return true;
        }

        // Global fallback: works before PlayerDeviceBinder finishes pairing.
        for (int i = 0; i < Gamepad.all.Count; i++)
        {
            Gamepad gamepad = Gamepad.all[i];
            if (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                return true;
        }

        for (int i = 0; i < Joystick.all.Count; i++)
        {
            Joystick joystick = Joystick.all[i];
            if (joystick == null || joystick is Gamepad)
                continue;

            if (joystick.trigger != null && joystick.trigger.wasPressedThisFrame)
                return true;

            ButtonControl button1 = joystick.TryGetChildControl<ButtonControl>("button1");
            if (button1 != null && button1.wasPressedThisFrame)
                return true;
        }

        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
    }
}
