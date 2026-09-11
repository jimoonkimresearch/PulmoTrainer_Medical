using System.Collections.Generic;
using UnityEngine;
using PulmoTrainer.Bluetooth;

namespace PulmoTrainer.UI
{
    // UI visibility only. Connection monitoring and sensor delivery remain in BluetoothManager.
    [DisallowMultipleComponent]
    public sealed class PulmoStatusVisibility : MonoBehaviour
    {
        private PulmoBluetoothStatusView view;
        private readonly List<GameObject> blockingPanels = new List<GameObject>();
        public void Configure(PulmoBluetoothStatusView statusView, IEnumerable<GameObject> panels)
        {
            view = statusView;
            blockingPanels.Clear();
            foreach (GameObject panel in panels)
                if (panel != null && !blockingPanels.Contains(panel)) blockingPanels.Add(panel);
            Refresh();
        }
        private void LateUpdate() { Refresh(); }
        private void Refresh()
        {
            if (view == null) return;
            foreach (GameObject panel in blockingPanels)
                if (panel != null && panel.activeInHierarchy)
                {
                    view.SetVisible(false);
                    return;
                }
            view.SetVisible(true);
        }
    }
}
