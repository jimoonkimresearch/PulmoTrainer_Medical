using System;
using UnityEngine;
using PulmoTrainer.Bluetooth;

namespace PulmoTrainer.UI
{
    // UI visibility only. Connection monitoring and sensor delivery remain in BluetoothManager.
    [DisallowMultipleComponent]
    public sealed class PulmoStatusVisibility : MonoBehaviour
    {
        [Flags]
        public enum Blocker { None = 0, Guide = 1, Score = 2, EyeBlocker = 4 }

        private PulmoBluetoothStatusView view;
        private Blocker blockers;

        public void ResetForScene(PulmoBluetoothStatusView statusView)
        {
            view = statusView;
            blockers = Blocker.None;
            Refresh();
        }

        public void SetBlocked(Blocker blocker, bool blocked)
        {
            Blocker next = blocked ? blockers | blocker : blockers & ~blocker;
            if (next == blockers) return;
            blockers = next;
            Refresh();
        }

        private void Refresh()
        {
            if (view != null) view.SetVisible(blockers == Blocker.None);
        }
    }
}
