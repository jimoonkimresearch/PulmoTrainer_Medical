using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace PulmoTrainer.Bluetooth
{
    // Permission policy for the bundled ArduinoBluetoothAPI, independent of scene UI.
    internal sealed class PulmoBluetoothPermissions
    {
        public bool IsGranted { get; private set; }

        public IEnumerator RequestAccess()
        {
            IsGranted = false;
#if UNITY_ANDROID && !UNITY_EDITOR
            int sdk;
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                sdk = version.GetStatic<int>("SDK_INT");

            // The legacy Java scanner checks BOTH location grants even on Android 12+.
            // Keep foreground location until that scanner is replaced. Never request background location.
            yield return RequestMissing(new[] { Permission.CoarseLocation, Permission.FineLocation });
            if (!HasAll(new[] { Permission.CoarseLocation, Permission.FineLocation }))
                yield break;

            if (sdk >= 31)
            {
                var nearby = new[] { "android.permission.BLUETOOTH_SCAN", "android.permission.BLUETOOTH_CONNECT" };
                yield return RequestMissing(nearby);
                if (!HasAll(nearby)) yield break;
            }
#endif
            IsGranted = true;
            yield break;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static bool HasAll(string[] permissions)
        {
            foreach (string permission in permissions)
                if (!Permission.HasUserAuthorizedPermission(permission)) return false;
            return true;
        }

        private static IEnumerator RequestMissing(string[] permissions)
        {
            if (HasAll(permissions)) yield break;

            // Request coarse and fine together: Android 12 requires the pair for precise location.
            var pending = new HashSet<string>(permissions);
            bool denied = false;
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += permission => pending.Remove(permission);
            callbacks.PermissionDenied += permission => { denied = true; pending.Remove(permission); };
            Permission.RequestUserPermissions(permissions, callbacks);
            while (pending.Count > 0 && !denied) yield return null;
        }
#endif
    }
}
