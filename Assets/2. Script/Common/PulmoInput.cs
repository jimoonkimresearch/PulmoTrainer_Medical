using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public static class PulmoInput
{
    private static bool InputEnabled => !PulmoTrainer.Training.TrainingEndSequence.IsInputBlocked;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    // 기존 Input.touchCount > 0에 대응.
    // 터치가 끝난 프레임도 포함할 수 있다.
    public static bool TouchPresent =>
        InputEnabled && ETouch.activeTouches.Count > 0;

    // 마우스 왼쪽 버튼 또는 화면 터치를 뗀 프레임.
    public static bool PointerReleased
    {
        get
        {
            if (!InputEnabled) return false;
            if (Mouse.current?.leftButton.wasReleasedThisFrame ?? false)
                return true;

            var touches = ETouch.activeTouches;

            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].phase ==
                    UnityEngine.InputSystem.TouchPhase.Ended)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public static bool AHeld =>
        InputEnabled && (Keyboard.current?.aKey.isPressed ?? false);

    public static bool SHeld =>
        InputEnabled && (Keyboard.current?.sKey.isPressed ?? false);

    public static bool DHeld =>
        InputEnabled && (Keyboard.current?.dKey.isPressed ?? false);

    public static bool DPressed =>
        InputEnabled && (Keyboard.current?.dKey.wasPressedThisFrame ?? false);

    public static bool SpacePressed =>
        InputEnabled && (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false);

    public static bool Digit1Pressed =>
        InputEnabled && (Keyboard.current?.digit1Key.wasPressedThisFrame ?? false);

    // Keep serialized KeyCode bindings while reading exclusively through the Input System.
    private static readonly Dictionary<KeyCode, Key> keyboardKeyCache = new Dictionary<KeyCode, Key>();

    public static bool KeyHeld(KeyCode keyCode)
    {
        return TryGetKeyboardKey(keyCode, out var keyboard, out var key) && keyboard[key].isPressed;
    }

    public static bool KeyPressed(KeyCode keyCode)
    {
        return TryGetKeyboardKey(keyCode, out var keyboard, out var key) && keyboard[key].wasPressedThisFrame;
    }

    private static bool TryGetKeyboardKey(KeyCode keyCode, out Keyboard keyboard, out Key key)
    {
        keyboard = Keyboard.current;
        key = Key.None;
        if (!InputEnabled || keyboard == null || keyCode == KeyCode.None) return false;
        if (keyboardKeyCache.TryGetValue(keyCode, out key)) return key != Key.None;

        string name = keyCode.ToString();
        // The two enums have different numeric values; never cast between them.
        if (name.StartsWith("Alpha", StringComparison.Ordinal))
            name = "Digit" + name.Substring(5);
        else if (name.StartsWith("Keypad", StringComparison.Ordinal))
            name = "Numpad" + name.Substring(6);
        else
        {
            switch (name)
            {
                case "Return": name = "Enter"; break;
                case "LeftControl": name = "LeftCtrl"; break;
                case "RightControl": name = "RightCtrl"; break;
                case "LeftCommand":
                case "LeftApple":
                case "LeftWindows": name = "LeftMeta"; break;
                case "RightCommand":
                case "RightApple":
                case "RightWindows": name = "RightMeta"; break;
                case "Print": name = "PrintScreen"; break;
                case "Break": name = "Pause"; break;
                case "Menu": name = "ContextMenu"; break;
            }
        }

        if (!Enum.IsDefined(typeof(KeyCode), keyCode) ||
            !Enum.TryParse(name, true, out key) || !Enum.IsDefined(typeof(Key), key) || key == Key.None)
        {
            key = Key.None;
            Debug.LogWarning("PulmoInput: unsupported keyboard binding " + keyCode +
                ". Select a keyboard key in the Inspector.");
        }
        keyboardKeyCache[keyCode] = key; // Unsupported bindings warn only once, too.
        return key != Key.None;
    }
}
