using UnityEngine;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
using Google.XR.Cardboard;
using UnityEngine.XR;
using UnityEngine.XR.Management;
#endif

namespace PulmoTrainer.Cardboard
{
    [DisallowMultipleComponent]
    public sealed class PulmoCardboardRuntime : MonoBehaviour
    {
        [Tooltip("Enable for the first device test to configure the viewer using its QR code.")]
        [SerializeField] private bool scanViewerOnFirstUse = false;
        [Tooltip("Cardboard overlay X button exits the app when enabled.")]
        [SerializeField] private bool allowCloseButtonToQuit = true;

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        private static PulmoCardboardRuntime active;
        private bool started;
        private int previousSleepTimeout;

        private void OnEnable()
        {
            if (active != null && active != this)
            {
                Debug.LogError("Only one PulmoCardboardRuntime can be active.", this);
                enabled = false;
                return;
            }
            active = this;
            previousSleepTimeout = Screen.sleepTimeout;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            XRManagerSettings manager = XRGeneralSettings.Instance != null
                ? XRGeneralSettings.Instance.Manager : null;
            // Never call native Cardboard APIs with a different or absent XR loader.
            if (manager == null || !manager.isInitializationComplete || manager.activeLoader == null ||
                !(manager.activeLoader is Google.XR.Cardboard.XRLoader)) return;
            XRDisplaySubsystem display = manager.activeLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
            if (display == null || !display.running) return;

            if (!started)
            {
                started = true;
                if (scanViewerOnFirstUse && !Api.HasDeviceParams()) Api.ScanDeviceParams();
            }
            if (Api.IsGearButtonPressed) Api.ScanDeviceParams();
            if (allowCloseButtonToQuit && Api.IsCloseButtonPressed) Application.Quit();
            if (Api.IsTriggerHeldPressed) Api.Recenter();
            if (Api.HasNewDeviceParams()) Api.ReloadDeviceParams();
            Api.UpdateScreenParams();
        }

        private void OnDisable()
        {
            if (active != this) return;
            Screen.sleepTimeout = previousSleepTimeout;
            active = null;
        }
#endif
    }
}
