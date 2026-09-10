using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public static class PulmoInput
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    // 기존 Input.touchCount > 0에 대응.
    // 터치가 끝난 프레임도 포함할 수 있다.
    public static bool TouchPresent =>
        ETouch.activeTouches.Count > 0;

    // 마우스 왼쪽 버튼 또는 화면 터치를 뗀 프레임.
    public static bool PointerReleased
    {
        get
        {
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
        Keyboard.current?.aKey.isPressed ?? false;

    public static bool SHeld =>
        Keyboard.current?.sKey.isPressed ?? false;

    public static bool DHeld =>
        Keyboard.current?.dKey.isPressed ?? false;

    public static bool DPressed =>
        Keyboard.current?.dKey.wasPressedThisFrame ?? false;

    public static bool SpacePressed =>
        Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;

    public static bool Digit1Pressed =>
        Keyboard.current?.digit1Key.wasPressedThisFrame ?? false;
}