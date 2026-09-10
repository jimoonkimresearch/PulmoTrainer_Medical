using UnityEngine;
using UnityEngine.SpatialTracking;
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace PulmoTrainer.Cardboard
{
    // Attach only to Player/Main Camera. Parent locomotion stays with the game.
    [DefaultExecutionOrder(-31000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class PulmoHeadPose : MonoBehaviour
    {
        [Header("Editor: Alt + mouse = look; Ctrl + mouse = roll")]
        [SerializeField] private float legacyYawSensitivity = 5f;
        [SerializeField] private float legacyPitchSensitivity = 2.4f;
        [SerializeField] private float pixelYawSensitivity = 0.5f;
        [SerializeField] private float pixelPitchSensitivity = 0.24f;

        private TrackedPoseDriver ownedDriver;
#if UNITY_EDITOR
        private Quaternion initialRotation;
        private float yaw;
        private float pitch;
        private float roll;
#endif

        private void Awake()
        {
            // Do not silently take over another component's tracking configuration.
            foreach (Behaviour component in GetComponents<Behaviour>())
            {
                if (component != null && component.GetType().Name == "TrackedPoseDriver")
                {
                    Debug.LogError("PulmoHeadPose: remove the existing TrackedPoseDriver " +
                        "from this camera before using this component.", this);
                    enabled = false;
                    return;
                }
            }
#if UNITY_EDITOR
            initialRotation = transform.localRotation;
#elif UNITY_ANDROID || UNITY_IOS
            ownedDriver = gameObject.AddComponent<TrackedPoseDriver>();
            ownedDriver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice,
                TrackedPoseDriver.TrackedPose.Center);
            ownedDriver.trackingType = TrackedPoseDriver.TrackingType.RotationOnly;
            ownedDriver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
            ownedDriver.UseRelativeTransform = false;
            ownedDriver.enabled = isActiveAndEnabled;
#endif
        }

        private void OnEnable()
        {
            if (ownedDriver != null) ownedDriver.enabled = true;
        }

        private void OnDisable()
        {
            if (ownedDriver != null) ownedDriver.enabled = false;
        }

        private void OnDestroy()
        {
            if (ownedDriver != null) Destroy(ownedDriver);
        }

#if UNITY_EDITOR
        private void Update()
        {
            bool alt = false;
            bool ctrl = false;
            Vector2 delta = Vector2.zero;
            float horizontal = legacyYawSensitivity;
            float vertical = legacyPitchSensitivity;
            if (Application.isFocused)
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
                ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
                if (alt || ctrl)
                    delta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#elif ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                Mouse mouse = Mouse.current;
                if (keyboard != null && mouse != null)
                {
                    alt = keyboard.leftAltKey.isPressed || keyboard.rightAltKey.isPressed;
                    ctrl = keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed;
                    if (alt || ctrl) delta = mouse.delta.ReadValue();
                }
                horizontal = pixelYawSensitivity;
                vertical = pixelPitchSensitivity;
#endif
            }

            if (alt)
            {
                yaw = Mathf.Repeat(yaw + delta.x * horizontal + 180f, 360f) - 180f;
                pitch = Mathf.Clamp(pitch - delta.y * vertical, -85f, 85f);
            }
            else if (ctrl)
            {
                roll = Mathf.Clamp(roll + delta.x * horizontal, -85f, 85f);
            }

            if (alt || !ctrl)
                roll = Mathf.Lerp(roll, 0f, Time.unscaledDeltaTime / (Time.unscaledDeltaTime + 0.1f));

            transform.localRotation = initialRotation * Quaternion.Euler(pitch, yaw, roll);
        }
#endif
    }
}
