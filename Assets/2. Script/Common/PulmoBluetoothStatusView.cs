using UnityEngine;
using UnityEngine.UI;

namespace PulmoTrainer.Bluetooth
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public sealed class PulmoBluetoothStatusView : MonoBehaviour
    {
        private Text label;
        private void Awake() { label = GetComponent<Text>(); }
        public void Initialize(Text text) { label = text; }
        public void SetStatus(string status)
        {
            if (label == null) label = GetComponent<Text>();
            if (label != null && label.text != status) label.text = status;
        }
        public void SetVisible(bool visible)
        {
            if (label == null) label = GetComponent<Text>();
            if (label != null) label.enabled = visible;
        }
    }
}
